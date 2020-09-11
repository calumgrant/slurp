using System;
using System.Collections.Generic;
using System.Linq;

namespace Parser
{
    /// <summary>
    /// A symbol in a grammar.
    /// Symbols are either "terminal" or "non-terminal".
    /// Terminal symbols come from the tokenizer, whereas
    /// non-terminal symbols have "production rules" in the grammar.
    /// </summary>
    public interface ISymbol
    {
        /// <summary>
        /// The set of terminal symbols.
        /// TODO: What about First_k?
        /// </summary>
        HashSet<ITerminalSymbol> First { get; }

        /// <summary>
        /// True if this symbol can be empty, i.e. has a production rule
        /// of the form "A -> {}", or a rule of the form
        /// "A -> B C D..." where B,C,D are all potentially empty.
        /// 
        /// Always false for terminal symbols.
        /// </summary>
        bool CanBeEmpty { get; set; }

        /// <summary>
        /// True if this symbol is a terminal symbol.
        /// </summary>
        bool IsTerminal { get; }
    }

    public interface ITerminalSymbol : ISymbol<Token>
    {
        int TerminalIndex { get; set; }
    }

    public interface INonterminalSymbol : ISymbol
    {
        int GotoIndex { get; set; }

        IEnumerable<ProductionRule> Rules { get; }
    }

    public interface INonterminalSymbol<T> : INonterminalSymbol, ISymbol<T>
    {
    }

    public interface ISymbol<Result> : ISymbol
    {
    }

    delegate void ParseAction(Token nextToken, IParseActions parser);

    delegate void Reduce(Token nextToken, IParseActions parser);

    /// <summary>
    /// A grammar symbol, representing a non-terminal symbolin the grammar.
    /// 
    /// Symbols specify their "result type"
    /// </summary>
    /// <typeparam name="Result">When a parse is successful, this is the type of the result.</typeparam>
    public class Symbol<Result> : INonterminalSymbol<Result>
    {
        // The name of the symbol.
        // This isn't needed to compile the grammar, but can come in useful
        // when debugging the grammar tables.
        // !! Conditional compilation
        readonly string debugName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">To print nicely, supply a string here naming the symbol.</param>
        public Symbol(string name = "?") { debugName = name; }

        public override string ToString() => debugName;

        public bool IsTerminal => false;

        public int Index { get; set; }

        public bool CanBeEmpty { get; set; }

        List<ProductionRule> rules = new List<ProductionRule>();

        private void Match(Reduce fn, params ISymbol[] symbols) { rules.Add(new ProductionRule(this, symbols, fn)); }

        public void Match(Symbol<Result> r) => Match(r, x=>x);

        public void Match(Func<Result> fn) => Match((token,parser) => parser.Reduce(token, fn(), this));

        public void Match<T>(ISymbol<T> r, Func<T, Result> fn) => Match((token,parser) => parser.Reduce(token, fn((T)parser.Pop()), this), r);

        public void Match<T1, T2>(ISymbol<T1> r1, ISymbol<T2> r2, Func<T1, T2, Result> fn)
        {
            Match( (token,parser) =>
            {
                var b = (T2)parser.Pop();
                var a = (T1)parser.Pop();
                parser.Reduce(token,fn(a, b), this);
            }, r1, r2);
        }

        public void Match<T1, T2, T3>(ISymbol<T1> r1, ISymbol<T2> r2, ISymbol<T3> r3, Func<T1, T2, T3, Result> fn)
        {
            Match((token,stack) =>
            {
                var c = (T3)stack.Pop();
                var b = (T2)stack.Pop();
                var a = (T1)stack.Pop();
                stack.Reduce(token, fn(a, b, c), this);
            }, r1, r2, r3);
        }

        public IParser<Result> MakeParser(ParserGenerator algorithm) => new Parser<Result>(this, algorithm);

        public IEnumerable<ProductionRule> Rules => rules;

        public IEnumerable<ISymbol> ReachableSymbols
        {
            get
            {
                var visited = new HashSet<ISymbol>();
                var toVisit = new Stack<INonterminalSymbol>();
                toVisit.Push(this);
                visited.Add(this);

                while(toVisit.Count>0)
                {
                    var current = toVisit.Pop();
                    foreach(var s in current.Rules.SelectMany(r => r.rhs))
                    {
                        if(!visited.Contains(s))
                        {
                            visited.Add(s);
                            if(s is INonterminalSymbol nt)
                                toVisit.Push(nt);
                        }
                    }
                }
                return visited;
            }
        }

        public IEnumerable<Terminal> ReachableTerminals => ReachableSymbols.OfType<Terminal>();

        public int GotoIndex { get; set; }

        public HashSet<ITerminalSymbol> First { get; } = new HashSet<ITerminalSymbol>();

        public HashSet<ITerminalSymbol> Follows { get; } = new HashSet<ITerminalSymbol>();

        public Tokenizer MakeTokenizer(params Terminal[] whitespace)
        {
            return new Tokenizer(ReachableTerminals.ToArray());
        }
    }
}
