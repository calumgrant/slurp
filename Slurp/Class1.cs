using System;
using System.Collections.Generic;
using System.Linq;

namespace Slurp
{
    public class SyntaxError : Exception
    {
        public SyntaxError(Token errorToken, IEnumerable<ITerminalSymbol> expected) : base($"Syntax error at {errorToken.Row}:{errorToken.Column}")
        {
            ErrorToken = errorToken;
            ExpectedSymbols = expected;
        }

        public IEnumerable<ITerminalSymbol> ExpectedSymbols { get; }

        public Token ErrorToken { get; }
    }

    namespace DFA
    {
        /// <summary>
        /// A state in a Deterministic Finite Automaton.
        /// 
        /// 
        /// </summary>
        interface IAutomaton
        {

            IAutomaton Next(int i);

            /// <summary>
            /// Holds if this is an accepting state.
            /// </summary>
            bool Accepts { get; }

            /// <summary>
            /// Holds if this machine can be "empty".
            /// </summary>
            bool Empty { get; }
        }

        sealed class TokenizerState : IAutomaton
        {
            public TokenizerState(IAutomaton[] t) { tokens = t; }

            IAutomaton[] tokens;

            public bool Accepts => tokens.Any(t => t.Accepts);

            public bool Empty => tokens.Any(t => t.Empty);

            public TokenizerState Next(int i)
            {
                var result = tokens.Select(t => t.Next(i)).ToArray();
                return new TokenizerState(result);
            }

            public int AcceptToken
            {
                get
                {
                    bool hasAnAccept = false;
                    for (int i = 0; i < tokens.Length; ++i)
                    {
                        if (tokens[i].Accepts) return i;

                        if (!(tokens[i] is RejectState)) hasAnAccept = true;
                    }
                    return hasAnAccept ? -1 : -2;
                }
            }

            public override int GetHashCode()
            {
                int h = 666;
                foreach(var t in tokens)
                {
                    h = 321 + 17 * t.GetHashCode();
                }
                return h;
            }

            public override bool Equals(object other)
            {
                return other is TokenizerState ts && tokens.SequenceEqual(ts.tokens);
            }

            IAutomaton IAutomaton.Next(int i) => Next(i);
        }

        sealed class RejectState : IAutomaton
        {
            public override bool Equals(object other)
            {
                return other is RejectState;
            }

            public override int GetHashCode()
            {
                return 54321;
            }

            public bool Accepts => false;

            public IAutomaton Next(int i) => this;

            public static RejectState Instance = new RejectState();

            // Always use `Instance`
            private RejectState() { }

            public bool Empty => false;
        }

        sealed class EmptyState : IAutomaton
        {
            public override bool Equals(object other)
            {
                return other is EmptyState;
            }

            public override int GetHashCode()
            {
                return 5434;
            }

            public bool Accepts => true;

            public IAutomaton Next(int i) => RejectState.Instance;

            private EmptyState() { }

            public static EmptyState Instance = new EmptyState();

            public bool Empty => true;
        }

        sealed class CharState : IAutomaton
        {
            readonly int Char;

            private CharState(int ch) { Char = ch; }

            public static IAutomaton Create(int ch)
            {
                return states[ch];
            }

            private static CharState[] states;

            static CharState()
            {
                states = new CharState[16];
                for (int i = 0; i < 16; ++i)
                    states[i] = new CharState(i);
            }

            public override bool Equals(object other)
            {
                return other is CharState cs && cs.Char == Char;
            }

            public override int GetHashCode()
            {
                return 54399 + Char * 13;
            }

            public IAutomaton Next(int i) => Char == i ? EmptyState.Instance : (IAutomaton)RejectState.Instance;

            public bool Accepts => false;

            public bool Empty => false;
        }

        sealed class CharRangeState : IAutomaton
        {
            readonly int Char1, Char2;

            private CharRangeState(int ch1, int ch2) { Char1 = ch1; Char2 = ch2; }

            public static IAutomaton Create(int ch1, int ch2)
            {
                if (ch1 == 0 && ch2 == 15) return AnyCharState.Instance;
                return ch1 == ch2 ? CharState.Create(ch1) : (IAutomaton)new CharRangeState(ch1, ch2);
            }

            public override bool Equals(object other)
            {
                return other is CharRangeState cs && cs.Char1 == Char1 && cs.Char2 == Char2;
            }

            public override int GetHashCode()
            {
                return 54399 + 47 * (Char1 * 13 + Char2);
            }

            public IAutomaton Next(int i) => i>=Char1 && i<=Char2 ? EmptyState.Instance : (IAutomaton)RejectState.Instance;

            public bool Accepts => false;

            public bool Empty => false;
        }


        sealed class NotState : IAutomaton
        {
            public bool Accepts => false;

            public bool Empty => false;

            public IAutomaton Next(int i)
            {
                var next = State.Next(i);
                if (next is EmptyState) return RejectState.Instance;
                if (next is RejectState) return EmptyState.Instance;

                return Create(next);
            }

            readonly IAutomaton State;

            private NotState(IAutomaton s) { State = s; }

            public static IAutomaton Create(IAutomaton s)
            {
                if (s is NotState ns) return ns.State;

                return new NotState(s);
            }

            public override bool Equals(object obj)
            {
                return obj is NotState ns && State.Equals(ns.State);
            }

            public override int GetHashCode()
            {
                return 99 + 37 * State.GetHashCode();
            }
        }

        sealed class StarState : IAutomaton
        {
            readonly IAutomaton Child;

            IAutomaton unpacked;
            private StarState(IAutomaton a)
            {
                Child = a;
                unpacked = SeqState.Create(a, this);
            }

            public static IAutomaton Create(IAutomaton child)
            {
                // Prevent some infinite recursions
                if (child is EmptyState || child is StarState) return child;
                return new StarState(child);
            }

            public bool Accepts => true;

            public bool Empty => true;

            public IAutomaton Next(int i) => unpacked.Next(i);

            public override bool Equals(object other)
            {
                return other is StarState ss && Child.Equals(ss.Child);
            }

            public override int GetHashCode() => 888 + 13 + Child.GetHashCode();
        }

        sealed class OrState : IAutomaton
        {
            public readonly IAutomaton State1, State2;

            private OrState(IAutomaton s1, IAutomaton s2) { State1 = s1; State2 = s2; }

            public static IAutomaton Create(IAutomaton s1, IAutomaton s2)
            {
                // Attempt to simplify/canonicalise the resulting state as much as possible.
                if (s1 is RejectState) return s2;
                if (s2 is RejectState) return s1;
                if (s1.Equals(s2)) return s1;

                if(s1 is OrState os)
                {
                    return Create(os.State1, Create(os.State2, s2));
                }

                if (s1.GetHashCode() < s2.GetHashCode()) return new OrState(s2, s1);
                return new OrState(s1, s2);
            }

            public bool Accepts => State1.Accepts || State2.Accepts;

            public bool Empty => State1.Empty || State2.Empty;

            public IAutomaton Next(int i)
            {
                var n1 = State1.Next(i);
                var n2 = State2.Next(i);

                return Create(n1, n2);
            }

            public override bool Equals(object obj)
            {
                return obj is OrState seq && State1.Equals(seq.State1) && State2.Equals(seq.State2);
            }

            

            public override int GetHashCode() => 100 + State1.GetHashCode() + 11 * State2.GetHashCode();

        }

        sealed class AnyCharState : IAutomaton
        {
            public bool Accepts => false;

            public bool Empty => false;

            public IAutomaton Next(int i) => EmptyState.Instance;

            private AnyCharState() { }

            public static readonly AnyCharState Instance = new AnyCharState();
        }

        sealed class SeqState : IAutomaton
        {
            public static IAutomaton Create(IAutomaton s1, IAutomaton s2)
            {
                if (s1 is RejectState) return s1;
                if (s2 is RejectState) return s2;
                if (s1 is EmptyState) return s2;
                if (s2 is EmptyState) return s1;

                if(s1 is SeqState ss)
                {
                    return Create(ss.State1, Create(ss.State2, s2));
                }

                return new SeqState(s1, s2);
            }

            readonly IAutomaton State1, State2;

            private SeqState(IAutomaton s1, IAutomaton s2)
            {
                State1 = s1;
                State2 = s2;
            }

            public bool Accepts => State1.Accepts || (State1.Empty && State2.Accepts);

            public bool Empty => State1.Empty && State2.Empty;


            public override bool Equals(object obj)
            {
                return obj is SeqState seq && State1.Equals(seq.State1) && State2.Equals(seq.State2);
            }

            public override int GetHashCode() => 953 + State1.GetHashCode() + 61 * State2.GetHashCode();

            public IAutomaton Next(int i)
            {
                var s1 = State1.Next(i);
                if(State1.Empty)
                {
                    var s2 = State2.Next(i);
                    return OrState.Create(s2, SeqState.Create(s1, State2));
                }
                else
                {
                    return SeqState.Create(s1, State2);
                }
            }
        }
    }
}
