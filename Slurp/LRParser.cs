using System;
using System.Collections.Generic;
using System.Linq;

namespace Slurp
{
    /// <summary>
    /// A parser, that transforms a stream of characters into a resultant datatype.
    /// </summary>
    /// <typeparam name="Result">The data type to produce on a successful parse.</typeparam>
    public interface IParser<Result>
    {
        /// <summary>
        /// Gets the underlying tokenizer used by the parser.
        /// </summary>
        Tokenizer Tokenizer { get; }

        /// <summary>
        /// Parses an input stream.
        /// </summary>
        /// <param name="input">The characters to parse.</param>
        /// <exception cref="SyntaxError"></exception>
        /// <returns></returns>
        Result Parse(IEnumerable<char> input);
    }

    /// <summary>
    /// An algorithm for constructing an LR parser.
    /// Typical implementations include: LR(0), SLR(1), LALR(1) and CLR(1)
    /// </summary>
    interface ILRParserGenerator : IEqualityComparer<State>
    {
        State CreateInitialState(INonterminalSymbol s);

        ParseAction ParseAction(State s, ITerminalSymbol t);

        State CreateGoto(State state, ISymbol s);

        /// <summary>
        /// Closes the state by adding addition items that could be matched.
        /// </summary>
        /// <param name="state"></param>
        void ExpandClosure(State state);
    }

    /// <summary>
    /// Callback interface for parser actions (called during parsing).
    /// </summary>
    interface IParseActions
    {
        /// <summary>
        /// Pops a value from the stack, during rule reduction.
        /// </summary>
        /// <returns></returns>
        object Pop();

        /// <summary>
        /// Performs a reduce action.
        /// </summary>
        /// <param name="lookahead">The lookahead token.</param>
        /// <param name="value">The new value to push.</param>
        /// <param name="pushedSymbol">The symbol being pushed.</param>
        void Reduce(Token lookahead, object value, INonterminalSymbol pushedSymbol);


        /// <summary>
        /// Shifts a token onto the stack.
        /// </summary>
        /// <param name="token">The token to shift.</param>
        /// <param name="newState">The state to enter.</param>
        void Shift(Token token, State newState);

        /// <summary>
        /// Process a new token.
        /// </summary>
        /// <param name="token">The token to process.</param>
        void Accept(Token token);

        /// <summary>
        /// Report a parse error.
        /// </summary>
        /// <param name="token">The token at which the error occured.</param>
        void Error(Token token);
    }

    public enum ParserGenerator
    {
        LR0, SLR, LALR, CLR
    }

    public class ParserConstructionError : Exception
    {
        internal ParserConstructionError(string e) : base(e) { }
    }

    public class ReduceReduceConflict : ParserConstructionError
    {
        public ProductionRule Rule1 { get; }
        public ProductionRule Rule2 { get; }

        public ReduceReduceConflict(ProductionRule r1, ProductionRule r2) : base("Reduce-reduce conflict")
        {
            Rule1 = r1;
            Rule2 = r2;
        }
    }

    public class ShiftReduceConflict : ParserConstructionError
    {
        public ProductionRule ShiftRule { get; }
        public ProductionRule ReduceRule { get; }

        public ShiftReduceConflict(ProductionRule ruleToShift, ProductionRule ruleToReduce) : base("Shift-reduce conflict")
        {
            ShiftRule = ruleToShift;
            ReduceRule = ruleToReduce;
        }
    }

    public class Parser<Result> : IParser<Result>
    {
        public IEnumerable<Token> Tokenize(IEnumerable<char> sequence)
        {
            foreach(var tok in Tokenizer.Tokenize(sequence))
            {
                if (tok.TokenId >= 0) yield return tok;
            }
            yield return new Token("<eof>", eof.TerminalIndex);
        }

        public Result Parse(IEnumerable<char> sequence)
        {
            var instance = new ParseInstance(initialState);
            // var tmp = Tokenize(sequence).ToArray();
            foreach(var tok in Tokenize(sequence))
            {
                instance.Accept(tok);
            }
            instance.Pop(); // Shift off the $ token 

            return (Result)instance.Pop();
        }

        readonly Terminal eof, epsilon;
        readonly List<ProductionRule> rules;
        readonly List<Terminal> terminals;
        readonly List<ISymbol> symbols;
        readonly List<INonterminalSymbol> nonterminals;
        readonly Symbol<Result> startSymbol;


        // Various computations about each state

        private State initialState;

        private void MarkCanBeEmpty()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var s in nonterminals)
                {
                    if (!s.CanBeEmpty && s.Rules.Any(rule => rule.rhs.All(s => s.CanBeEmpty)))
                    {
                        s.CanBeEmpty = true;
                        changed = true;
                    }
                }
            }
            while (changed);
        }

        private void ComputeFirstSets()
        {
            foreach (var s in terminals)
            {
                s.First.Add(s);
            }

            bool changed;
            do
            {
                changed = false;
                foreach (var s in nonterminals)
                {
                    foreach (var r in s.Rules)
                    {
                        foreach (var s2 in r.rhs)
                        {
                            foreach (var f2 in s2.First)
                            {
                                if (!s.First.Contains(f2))
                                {
                                    s.First.Add(f2);
                                    changed = true;
                                }
                            }

                            if (!s2.CanBeEmpty)
                                break;
                        }
                    }
                }
            }
            while (changed);
        }

        ILRParserGenerator strategy;

        readonly Tokenizer tokenizer;

        public Tokenizer Tokenizer => tokenizer;

        public Parser(Symbol<Result> grammar, ParserGenerator algorithm, params Terminal[] whitespace)
        {
            strategy = algorithm switch
            {
                ParserGenerator.LR0 => new LR0ParserGenerator(),
                ParserGenerator.SLR => new SLRParserGenerator(),
                ParserGenerator.CLR => new CLRParserGenerator(),
                ParserGenerator.LALR => new LALRParserGenerator(),
                _ => throw new ArgumentException(nameof(algorithm))
            };

            visitedStates = new HashSet<State>(strategy);

            eof = Terminal.Eof;

            // Set up symbol metadata
            startSymbol = new Symbol<Result>("start");
            startSymbol.Match(grammar, eof, (x, y) => x);

            symbols = startSymbol.ReachableSymbols.ToList();
            terminals = grammar.ReachableTerminals.ToList();
            nonterminals = symbols.OfType<INonterminalSymbol>().ToList();

            tokenizer = grammar.MakeTokenizer(whitespace);
            terminals.Add(eof);  // Ensure it's at the end

            // Set up terminals
            epsilon = Terminal.Empty;  // !! Dont think this is needed ??

            // Mark potentially empty symbols
            MarkCanBeEmpty();

            ComputeFirstSets();

            // Index all the rules

            int index = 0;
            rules = new List<ProductionRule>();
            rules.AddRange(nonterminals.SelectMany(s => s.Rules));

            foreach (var r in rules)
            {
                r.Index = index++;
            }

            index = 0;
            foreach(var t in terminals)
            {
                t.TerminalIndex = index++;
            }

            index = 0;
            foreach (var t in nonterminals)
            {
                t.GotoIndex = index++;
            }

            // Compute the state tables

            // The initial state.

            initialState = strategy.CreateInitialState(startSymbol);
            strategy.ExpandClosure(initialState);
            visitedStates.Add(initialState);

            // Compute transitions for the state

            ComputeActions(initialState);
            ComputeGotos(initialState);
        }


        HashSet<State> visitedStates;

        private State Transition(State from, ISymbol symbol)
        {
            var to = strategy.CreateGoto(from, symbol);

            strategy.ExpandClosure(to);

            if (visitedStates.TryGetValue(to, out State existingState))
                return existingState;
            visitedStates.Add(to);
            ComputeActions(to);
            ComputeGotos(to);
            return to;
        }

        private void ComputeActions(State state)
        {
            state.terminalGotos ??= terminals.Select(t => Transition(state, t)).ToArray();

            /* https://en.wikipedia.org/wiki/LR_parser#Table_construction
                Constructing the action and goto tables
                From this table and the found item sets, the action and goto table are constructed as follows:

                1. The columns for nonterminals are copied to the goto table.
                2. The columns for the terminals are copied to the action table as shift actions.
                3. An extra column for '$' (end of input) is added to the action table that contains acc for every item set that contains an item of the form S → w • eof.
                4. If an item set i contains an item of the form A → w • and A → w is rule m with m > 0 then the row for state i in the action table is completely filled with the reduce action rm.
             */

            state.actions ??= terminals.Select(t => strategy.ParseAction(state, t)).ToArray();
        }

        private void ComputeGotos(State state)
        {
            state.nonterminalGotos ??= nonterminals.Select(t => Transition(state, t)).ToArray();
        }
    }


    /// <summary>
    /// State used when performing a parse.
    /// Important to keep this data outside of the "Parser" object to allow
    /// multiple threads to use the same parser at the same time.
    /// </summary>
    class ParseInstance : IParseActions
    {
        Stack<StackItem> stack;

        internal ParseInstance(State initialState)
        {
            stack = new Stack<StackItem>();
            stack.Push(new StackItem(initialState, null));
        }

        public State Current => stack.Peek().state;

        public void Accept(Token token)
        {
            Current.actions[token.TokenId](token, this);
        }

        public void Error(Token token)
        {
            throw new SyntaxError(token);
        }

        public object Pop() => stack.Pop().value;

        public void Reduce(Token next, object value, INonterminalSymbol pushedSymbol)
        {
            stack.Push(new StackItem(Current.nonterminalGotos[pushedSymbol.GotoIndex], value));
            Accept(next);
        }

        public void Shift(Token value, State target)
        {
            stack.Push(new StackItem(target, value));
        }

        private struct StackItem
        {
            public StackItem(State s, object v)
            {
                state = s;
                value = v;
            }
            public State state;
            public object value;
        }
    }
}
