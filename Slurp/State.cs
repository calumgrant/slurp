using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Slurp
{
    /// <summary>
    /// A "state" of an LR parser.
    /// An LR parser is a state machine, where at each point in the parsing sequence,
    /// a number of rules could be potentially matched. Tha "state" records which rules
    /// are currently being matched. Since there could be several candidate rules at any one time,
    /// the item-set records which rules are currenty under consideration.
    /// 
    /// In a look-ahead parser, each item must also match a "lookahead" token for it to be reduced.
    /// Rules are only reduced when the relevant lookahead tokens are present.
    /// </summary>
    sealed class State
    {
        // The item-set in the current state.
        internal HashSet<Item> items = new HashSet<Item>();

        // A vector 
        public bool[] ValidInputs;

        internal State()
        {
        }

        /// <summary>
        /// True if this is the "empty" state, meaning a syntax error has occurred
        /// and no more transitions are possible.
        /// </summary>
        public bool IsEmpty => items.Count == 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var i in items)
            {
                sb.Append(i.ToString());
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is State s)) return false;

            if (items.Count != s.items.Count) return false;

            return items.All(i => s.items.Contains(i));
        }

        public override int GetHashCode()
        {
           return items.Aggregate(0x4312, (r, v) => (r, v).GetHashCode());
        }

        // Map from terminal symbols to new states
        public State[] terminalGotos;

        // Map of non-terminal symbols to next state
        public State[] nonterminalGotos;

        public ParseAction[] actions;

        /// <summary>
        /// Gets the set of terminal symbols that could follow the nonterminal symbol s
        /// in the current item-set.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public IEnumerable<ITerminalSymbol> Follows(INonterminalSymbol s)
        {
            var result = new HashSet<ITerminalSymbol>();

            foreach (var e in items.Where(i => !i.AtEnd && i.NextSymbol == s))
            {
                ProductionRule rule = e.Rule;

                int index;
                for (index = e.DotPosition+1; index < rule.rhs.Length; ++index)
                {
                    foreach (var t in rule.rhs[index].First)
                        result.Add(t);
                    if (!rule.rhs[index].CanBeEmpty) break;
                }

                if(index == rule.rhs.Length)
                {
                    // This wasn't documented properly I don't think
                    // Also add the lookahead of the matched item if the rule can match at the end of the rule.
                    result.Add(e.Lookahead[0]);
                }
            }

            return result;
        }
    }
}
