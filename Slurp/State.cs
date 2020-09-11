using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parser
{
    sealed class State
    {
        internal HashSet<Item> items = new HashSet<Item>();

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

        public IEnumerable<ISymbol> Follows
        {
            get
            {
                foreach (var e in items)
                {
                    ProductionRule rule = e.Rule;

                    for (int index = e.DotPosition; index < rule.rhs.Length; ++index)
                    {
                        yield return rule.rhs[index];
                        if (!rule.rhs[index].CanBeEmpty) break;
                    }
                }
            }
        }
    }
}
