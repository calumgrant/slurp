using System.Linq;

namespace Slurp
{
    class LALRParserGenerator : LR0ParserGenerator
    {
        public override ParseAction ParseAction(State state, ITerminalSymbol symbol)
        {
            if (state.IsEmpty)
                return SyntaxError;

            // If any of the rules allow a shift, do that (ignoring potential reduces)
            if (state.items.Any(i => !i.AtEnd))
            {
                // TODO: Count shift/reduce conflicts
                // Shift the symbol onto the stack
                return (token, parser) => parser.Shift(token, state.terminalGotos[symbol.TerminalIndex]);
            }

            // We have one reduce rule
            if (state.items.Count == 1 && state.items.First().AtEnd)
            {
                // This is a "reduce" action
                return (token, parser) => state.items.First().Rule.function(token, parser);
            }

            // We have more than one reduce rule:
            throw new ReduceReduceConflict(state.items.First().Rule, state.items.ElementAt(1).Rule);
        }
    }
}
