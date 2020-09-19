using System;
using System.Linq;
using System.Text;

namespace Slurp
{
    /// <summary>
    /// An "item" in a item-set.
    /// This is a production rule, a "dot position" within the rule, and a lookahead token.
    /// 
    /// For example, S -> E . T, $
    /// 
    /// is a production rule, with a "dot" as position 1, meaning that the parser has
    /// processed an "E" and is now at the start of "T".
    /// The "$" means that the $ token should follow this rule for the rule to be reduced.
    /// 
    /// The "lookahead token" is not very well explained in the literature.
    /// The "lookahead" is the token that needs to follow this rule for the rule
    /// to be reduced.
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

        // The lookahead symbols are a list (generally of length = 1) of terminals that must be in the lookahead
        // buffer when we reduce the rule.
        // Lookahead is the sequence (of length=1) of terminals that follow this item.
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
