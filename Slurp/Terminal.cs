using Slurp.DFA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Slurp
{
    public sealed class Terminal : ITerminalSymbol
    {
        internal readonly DFA.IAutomaton automaton;

        public static Terminal Char(char ch) => new Terminal(CreateCharState(ch), ch.ToString());

        public static Terminal AnyChar => new Terminal(DFA.SeqState.Create(DFA.AnyCharState.Instance, DFA.SeqState.Create(DFA.AnyCharState.Instance, DFA.SeqState.Create(DFA.AnyCharState.Instance, DFA.AnyCharState.Instance))), "*");

        public static Terminal Eof => new Terminal(DFA.RejectState.Instance, "$");

        public static Terminal Empty = new Terminal(DFA.EmptyState.Instance, "epsilon");

        public static Terminal OneOf(Terminal ch1, params Terminal[] chars)
        {
            Terminal result = ch1;
            foreach(var ch in chars)
            {
                result = ch | result;
            }
            return result;
        }

        public static Terminal operator !(Terminal t) => new Terminal(DFA.NotState.Create(t.automaton), "^...");

        private Terminal(DFA.IAutomaton state, string debug = "?")
        {
            automaton = state;
            debugText = debug;
        }

        public override string ToString() => debugText;

        readonly string debugText;

        public static Terminal Seq(Terminal t1, Terminal t2)
        {
            return new Terminal(DFA.SeqState.Create(t1.automaton, t2.automaton));
        }

        public static implicit operator Terminal(char ch) => Char(ch);

        public static implicit operator Terminal(string str) => String(str);

        public static Terminal Or(Terminal t1, Terminal t2) => new Terminal(DFA.OrState.Create(t1.automaton, t2.automaton));

        public static Terminal operator +(Terminal t1, Terminal t2) => Seq(t1, t2);

        public static Terminal operator +(string s, Terminal t2) => String(s) + t2;

        public static Terminal String(string s) => new Terminal(CreateString(s), s);

        public static Terminal operator |(Terminal t1, Terminal t2) => Or(t1, t2);

        static DFA.IAutomaton CreateCharState(char s)
        {
            int charValue = (int)s;
            var s1 = DFA.CharState.Create((charValue >> 12) & 15);
            var s2 = DFA.CharState.Create((charValue >> 8) & 15);
            var s3 = DFA.CharState.Create((charValue >> 4) & 15);
            var s4 = DFA.CharState.Create(charValue & 15);

            return DFA.SeqState.Create(s1, DFA.SeqState.Create(s2, DFA.SeqState.Create(s3, s4)));
        }

        public Terminal Repeat(Range r)
        {
            IAutomaton seq = DFA.EmptyState.Instance;

            for (int i = 0; i < r.Start.Value; ++i)
                seq = DFA.SeqState.Create(automaton, seq);

            if(r.End.IsFromEnd)
            {
                seq = DFA.SeqState.Create(seq, StarState.Create(automaton));
            }
            else
            {
                for (int i = r.Start.Value; i < r.End.Value; ++i)
                    seq = DFA.SeqState.Create(seq, DFA.OrState.Create(DFA.EmptyState.Instance, automaton));
            }

            return new Terminal(seq);
        }

        public Terminal Not => new Terminal(DFA.NotState.Create(automaton));

        static DFA.IAutomaton CreateRangeState(char s1, char s2)
        {
            return CreateRange((int)s1, (int)s2, 12);
        }

        static DFA.IAutomaton CreateRange(int n1, int n2, int shift)
        {
            if (shift == 0)
            {
                return DFA.CharRangeState.Create(n1 & 15, n2 & 15);
            }

            int a = (n1 >> shift) & 15, b = (n2 >> shift) & 15;

            if (a == b)
            {
                return DFA.SeqState.Create(DFA.CharState.Create(a), CreateRange(n1, n2, shift - 4));
            }


            var split1 = DFA.SeqState.Create(DFA.CharState.Create(a), CreateRange(n1, 0xffff, shift - 4));
            var split2 = DFA.SeqState.Create(DFA.CharState.Create(b), CreateRange(0, n2, shift - 4));
            var bounds = DFA.OrState.Create(split1, split2);

            if (a + 1 == b)
            {
                // Split into two
                return bounds;
            }
            else
            {
                // Splits into three
                var midsplit = DFA.SeqState.Create(DFA.CharRangeState.Create(a + 1, b - 1), CreateRange(0, 0xffff, shift - 4));
                return DFA.OrState.Create(midsplit, bounds);
            }
        }

        public static Terminal Range(System.Range range) => Range((char)range.Start.Value, (char)range.End.Value);

        public static Terminal Range(char a, char b) => new Terminal(CreateRangeState((char)a, (char)b), $"[{a}-{b}]");

        private static DFA.IAutomaton CreateString(string s, int offset = 0, int shift = 12)
        {
            if (offset == s.Length) return DFA.EmptyState.Instance;

            var tail = CreateString(s, shift == 0 ? offset + 1 : offset, shift == 0 ? 12 : shift - 4);

            var head = DFA.CharState.Create((s[offset] >> shift) & 15);
            return DFA.SeqState.Create(head, tail);
        }

        static Terminal digit = Range('0'..'9');
        static Terminal lc = Range('a'..'z');
        static Terminal uc = Range('A'..'Z');
        static Terminal alpha = lc | uc;

        // These are only valid for ASCII, nothing else
        // TODO: Get the charater classes for UTF-8.

        public static Terminal Digit => digit;
        public static Terminal Alpha => alpha;

        // public Terminal Plus { get { return this; } }

        public bool IsTerminal => true;

        public int TerminalIndex { get; set; }

        public bool CanBeEmpty {  get { return false; } set { if (value) throw new ArgumentException(nameof(value)); } }

        public IEnumerable<ProductionRule> Rules => Enumerable.Empty<ProductionRule>();

        public override int GetHashCode() => automaton.GetHashCode();

        public override bool Equals(object obj)
        {
            return this is Terminal t && automaton.Equals(t.automaton);
        }

        public HashSet<ITerminalSymbol> First { get; } = new HashSet<ITerminalSymbol>();

        public HashSet<ITerminalSymbol> Follows { get; } = new HashSet<ITerminalSymbol>();

    }
}
