namespace Slurp
{
    /// <summary>
    /// A naive table generator that generates very large tables with many redundant states.
    /// </summary>
    class CLRParserGenerator : LR0ParserGenerator, ILRParserGenerator
    {
        public State CreateInitialState(INonterminalSymbol s)
        {
            var state = new State();
            foreach (var t in s.First)
            {
                foreach (var rule in s.Rules)
                {
                    state.items.Add(new Item(rule, 0, t));
                }
            }

            return state;
        }
    }
}
