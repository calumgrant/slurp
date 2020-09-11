using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Parser
{
    public class Token
    {
        public string Text;
        public int Position;
        public int Row, Column;
        public int TokenId;

        public override string ToString() => $"{Text} ({TokenId})";
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
            var sb = new StringBuilder();

            foreach(var ch in input)
            {
                var acceptToken = state.acceptToken;

                if(acceptToken == -2)
                {
                    // We are at a terminal reject state.
                    yield return new Token() { Text = sb.ToString(), TokenId = -1 };
                    sb.Clear();
                    state = initialState;
                }

                // Advance 4 positions
                state = state.transitions[(ch>>12)&15];
                state = state.transitions[(ch >> 8) & 15];
                state = state.transitions[(ch >> 4) & 15];
                state = state.transitions[ch & 15];

                if(acceptToken>=0 && state.acceptToken<0)
                {
                    var result = new Token() { Text = sb.ToString(), TokenId = acceptToken };
                    yield return result;

                    state = initialState.transitions[(ch >> 12) & 15];
                    state = state.transitions[(ch >> 8) & 15];
                    state = state.transitions[(ch >> 4) & 15];
                    state = state.transitions[ch & 15];
                    sb.Clear();
                }
                sb.Append(ch);
            }

            yield return new Token() { Text = sb.ToString(), TokenId = state.acceptToken>=0 ? state.acceptToken : -1 }; ;
        }
    }
}
