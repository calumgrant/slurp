using System.Linq;
using System.Text;

namespace Parser
{
    /// <summary>
    /// A grammar rule of the form
    /// 
    ///     A -> B C D ...
    /// 
    /// The right hand side of the rule can be empty.
    /// </summary>
    public class ProductionRule
    {
        public readonly ISymbol lhs;

        public readonly ISymbol[] rhs;

        // When the rule is matched, call this function that will 
        // pop symbols.Length objects from the stack, and push one object onto the stack.
        internal readonly Reduce function;

        public int Index;

        // Holds if the terminal can start this rule
        public bool Starts(ITerminalSymbol terminal)
        {
            foreach(var s in rhs)
            {
                if (s.First.Contains(terminal)) return true;
                if (!s.CanBeEmpty) return false;
            }
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(lhs.ToString());
            sb.Append(" ->");
            foreach (var s in rhs)
            {
                sb.Append(" ");
                sb.Append(s);
            }
            return sb.ToString();
        }

        internal ProductionRule(ISymbol target, ISymbol[] syms, Reduce fn)
        {
            lhs = target;
            rhs = syms;
            function = fn;
        }
    }
}
