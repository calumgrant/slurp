using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Slurp
{
    public class Token
    {
        public string Text;
        public int Position;
        public int Row, Column;
        public int TokenId;

        public override string ToString() => $"{Text} ({TokenId})";

        public Token(string text, int id)
        {
            Text = text;
            TokenId = id;
        }
    }

    public class UnmatchableTokenException : Exception
    {
        public UnmatchableTokenException(int n) : base($"Token {n} is unmatchable")
        {
            Token = n;
        }

        public int Token { get; }
    }

    public class Tokenizer
    {
        class State
        {
            public State[] transitions;  // An arry of 16 transitions to the next state
            public int acceptToken;  // -1 means, do not accept
        }

        Terminal[] terminals;

        Dictionary<DFA.TokenizerState, State> states = new Dictionary<DFA.TokenizerState, State>();
        readonly State initialState;

        public Tokenizer(params Terminal[] rules)
        {
            terminals = rules;
            var x = new DFA.TokenizerState(rules.Select(r => r.automaton).ToArray());

            initialState = AddState(x);

            // Detect if any rules are unmatched.
            var found = new bool[rules.Length];
            foreach(var state in states.Values)
            {
                if (state.acceptToken >= 0)
                    found[state.acceptToken] = true;
            }

            //
            for(int state=0; state < found.Length; ++state)
            {
                if (!found[state])
                    throw new UnmatchableTokenException(state);
            }
        }

        public int Match(string s)
        {
            int offset = 0;
            int shift = 12;
            State state = initialState;

            while (offset < s.Length)
            {
                int ch = (s[offset] >> shift) & 15;
                state = state.transitions[ch];

                if (shift == 0) { shift = 12; offset++; }
                else shift -= 4;
            }

            return state.acceptToken;
        }

        private State AddState(DFA.TokenizerState ts)
        {
            if (!states.TryGetValue(ts, out State state))
            {
                state = new State();
                states.Add(ts, state);

                // Populate this state
                state.transitions = new State[16];
                for (int i = 0; i < 16; ++i)
                {
                    state.transitions[i] = AddState(ts.Next(i));
                }

                state.acceptToken = ts.AcceptToken;
            }
            return state;
        }

        // TODO: A more efficient decoding

        public IEnumerable<Token> Tokenize(string str) => Tokenize((IEnumerable<char>)str);

        public IEnumerable<Token> Tokenize(IEnumerable<char> input)
        {
            State state = initialState;
            var sb = new StringBuilder();  // TODO: Use a deque
            int acceptLength = 0;
            int acceptToken = -1;

            void processChar(char ch)
            {
                state = state.transitions[(ch >> 12) & 15];
                state = state.transitions[(ch >> 8) & 15];
                state = state.transitions[(ch >> 4) & 15];
                state = state.transitions[ch & 15];
                sb.Append(ch);
            }

            foreach (var ch in input)
            {
                // -2 = reject
                // -1 = not accept, but a future match could accept this.

                processChar(ch);

                if (state.acceptToken == -2)
                {
                    // A terminal reject state.

                    var buf = sb.ToString();
                    if (acceptLength > 0)
                    {
                        yield return new Token(buf.Substring(0, acceptLength), acceptToken);
                    }

                    sb.Clear();
                    state = initialState;
                    acceptToken = -1;
                    int taillen = acceptLength;
                    acceptLength = 0;

                    // Re-scan the buffer (potentially expensive!)
                    for(int l = taillen; l<buf.Length; ++l)
                    {
                        processChar(buf[l]);
                        if(state.acceptToken == -2)
                        {
                            if (acceptLength > 0)
                            {
                                yield return new Token(sb.ToString().Substring(0, acceptLength), acceptToken);
                            }
                            else
                            {
                                yield return new Token(sb.ToString(), -1);
                            }
                            state = initialState;
                            acceptToken = -1;
                            acceptLength = 0;
                            sb.Clear();
                        }
                        else if(state.acceptToken>=0)
                        {
                            acceptLength++;
                            acceptToken = state.acceptToken;
                        }
                    }

                }
                else if(state.acceptToken >= 0)
                {
                    // An accept state

                    acceptLength = sb.Length;
                    acceptToken = state.acceptToken;
                }

            }

            if (acceptLength > 0)
            {
                yield return new Token(sb.ToString().Substring(0, acceptLength), acceptToken);
            }

            // Could potentially have an unmatched string at the end
        }
    }
}
