using System.Collections.Generic;
using System.Linq;

namespace Slurp
{
    /**
     */
    class LR0ParserGenerator : ILRParserGenerator
    {
        public State CreateInitialState(INonterminalSymbol s)
        {
            var state = new State();
            foreach (var rule in s.Rules)
                state.items.Add(new Item(rule, 0));

            ExpandClosure(state);
            return state;
        }

        public void ExpandClosure(State state)
        {
            var queue = new Queue<Item>(state.items);
            while (queue.TryDequeue(out var item))
            {
                if (!item.AtEnd)
                {
                    var s = item.NextSymbol;

                    // Add all the rules for the new symbol
                    if (s is INonterminalSymbol nt)
                    {
                        foreach (var rule in nt.Rules)
                        {
                            var newItem = new Item(rule, 0);
                            if (!state.items.Contains(newItem))
                            {
                                state.items.Add(newItem);
                                queue.Enqueue(newItem);
                            }
                        }
                    }
                }
            }
        }

        public State CreateGoto(State from, ISymbol next)
        {
            var s = new State();
            foreach(var item in from.items)
            {
                if (!item.AtEnd && item.NextSymbol == next)
                    s.items.Add(new Item(item.Rule, item.DotPosition + 1));
            }
            ExpandClosure(s);
            return s;
        }

        //

        static protected void SyntaxError(Token next, IParseActions actions)
        {
            actions.Error(next);
        }

        public virtual ParseAction ParseAction(State state, ITerminalSymbol symbol)
        {
            if (state.IsEmpty)
                return SyntaxError;

            // Are any of the items "reduce"
            if (state.items.Count==1 && state.items.First().AtEnd)
            {
                // This is a "reduce" action
                return (token, parser) => state.items.First().Rule.function(token, parser);
            }

            var atEndRules = state.items.Where(i => i.AtEnd);
            if (atEndRules.Count() > 1)
            {
                throw new ReduceReduceConflict(atEndRules.First().Rule, atEndRules.ElementAt(1).Rule);
            }

            // Detect shift/reduce conflict
            if (state.items.Count > 1 && atEndRules.Count() == 1)
            {
                // A shift/reduce conflict
                throw new ShiftReduceConflict(state.items.Where(i => !i.AtEnd).First().Rule, atEndRules.First().Rule);
            }

            // Shift the symbol onto the stack
            return (token, parser) => parser.Shift(token, state.terminalGotos[symbol.TerminalIndex]);
        }

        public bool Equals(State x, State y)
        {
            if (x.items.Count != y.items.Count) return false;

            return x.items.All(i => y.items.Contains(i));
        }

        public int GetHashCode(State obj)
        {
            return obj.items.Aggregate(0x4312, (r, v) => (r, v).GetHashCode());
        }
    }
}
