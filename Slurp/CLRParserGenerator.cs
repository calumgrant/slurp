using System.Collections.Generic;
using System.Linq;

namespace Slurp
{
    /// <summary>
    /// A naive table generator that generates very large tables with many redundant states.
    /// </summary>
    class CLRParserGenerator : ILRParserGenerator
    {
        public State CreateInitialState(INonterminalSymbol s)
        {
            // Hack to find the "eof" symbol
            ITerminalSymbol eof = (ITerminalSymbol)s.Rules.First().rhs[1];

            var state = new State();
            foreach (var rule in s.Rules)
            {
                state.items.Add(new Item(rule, 0, eof));
            }

            return state;
        }

        public void ExpandClosure(State state)
        {
            var queue = new Queue<Item>(state.items);
            while(queue.TryDequeue(out var item))
            {
                if(!item.AtEnd)
                {
                    if(item.NextSymbol is INonterminalSymbol nt)
                    {
                        // Assemble the list of "follows" symbols 
                        var follows = state.Follows(nt);

                        // Add the new items to the item-sets
                        foreach (var rule in nt.Rules)
                        {
                            foreach (var f in follows)
                            {
                                var newItem = new Item(rule, 0, f);
                                if(!state.items.Contains(newItem))
                                {
                                    state.items.Add(newItem);  // A non-kernel item
                                    queue.Enqueue(newItem);
                                }
                            }
                        }

                    }
                }
            }
        }

        public State CreateGoto(State state, ISymbol s)
        {
            var newState = new State();

            foreach(var item in state.items)
            {
                if(!item.AtEnd && item.NextSymbol == s)
                {
                    newState.items.Add(new Item(item.Rule, item.DotPosition + 1, item.Lookahead));
                }
            }

            return newState;
        }

        public bool Equals(State x, State y)
        {
            return x.Equals(y);
        }


        public int GetHashCode(State obj)
        {
            return obj.GetHashCode();
        }

        static void Error(Token next, IParseActions parser)
        {
            parser.Error(next);
        }

        public ParseAction ParseAction(State state, ITerminalSymbol symbol)
        {
            if(state.IsEmpty)
                return Error;

            var matchingItems = state.items.Where(i => i.AtEnd && i.Lookahead[0] == symbol);

            if(matchingItems.Count() == 1)
            {
                if (symbol.IsEof)
                    return (token, parser) =>
                    {
                        state.items.First().Rule.function(token, parser);
                        parser.ParseSuccess = true;
                    };

                return (token, parser) => state.items.First().Rule.function(token, parser);
            }

            if(matchingItems.Count() > 1)
            {
                throw new ReduceReduceConflict(matchingItems.ElementAt(0).Rule, matchingItems.ElementAt(1).Rule);
            }

            // Default is to shift the symbol onto the stack
            return (token, parser) => parser.Shift(token, state.terminalGotos[symbol.TerminalIndex]);
        }
    }
}
