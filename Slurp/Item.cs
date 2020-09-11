using System;
using System.Linq;
using System.Text;

namespace Slurp
{
    /// <summary>
    /// An "item" in a item-set.
    /// This is a production rule, a "dot position" within the rule, and a lookahead token.
    /// For example, S -> E . T
    /// </summary>
    sealed class Item
    {
        public Item(ProductionRule rule, int dotPosition, params ITerminalSymbol[] lookahead)
        {
            Rule = rule;
            DotPosition = dotPosition;
            Lookahead = lookahead;
        }

        public bool AtEnd => DotPosition >= Rule.rhs.Length;

        public ISymbol NextSymbol => Rule.rhs[DotPosition];

        public readonly ProductionRule Rule;
        public readonly int DotPosition;  // 0 indicates the first position in the rule.
        public readonly ITerminalSymbol[] Lookahead;  // 0 or 1 lookahead items  ?? Array or ITerminalSymbol?

        public override bool Equals(object obj)
        {
            return obj is Item item && Rule == item.Rule && DotPosition == item.DotPosition && Lookahead.SequenceEqual(item.Lookahead);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rule.GetHashCode(), DotPosition, Lookahead.Aggregate(31, (t, v) => HashCode.Combine(t, v)));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(Rule.lhs);
            sb.Append(" ->");

            int i = 0;
            foreach (var s in Rule.rhs)
            {
                if (i++ == DotPosition)
                    sb.Append(" .");
                sb.Append(' ');
                sb.Append(s.ToString());
            }
            if (i == DotPosition)
                sb.Append(" .");

            sb.Append(",");
            foreach (var l in Lookahead)
                sb.Append(' ').Append(l.ToString());

            sb.Append(']');
            // if (InKernel) sb.Append("*");
            return sb.ToString();
        }
    }

}
