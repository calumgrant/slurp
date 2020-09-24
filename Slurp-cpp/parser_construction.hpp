
namespace slurp
{
	template<typename S, typename Visited=ts_empty, bool Recursive=ts_contains<S,Visited>::value>
	struct reachable_symbols;

	template<typename S, typename Visited>
	struct reachable_symbols<S, Visited, true>
	{
		// A recursion guard: if we have looked for
		// reachable symbols in S already, then we do not
		// recursively attempt to add S to Visited.
		typedef Visited type;
	};

	template<typename S, typename Visited>
	struct reachable_symbols2;

	template<typename S, typename Visited>
	struct reachable_symbols<S, Visited, false>
	{
		typedef typename ts_concat<S, Visited>::type t;
		typedef typename reachable_symbols2<typename S::rule, t>::type type;
	};

	template<int N, typename Visited>
	struct reachable_symbols2<Rule<N>, Visited>
	{
		typedef Visited type;
	};


	template<int N, typename H, typename Visited, typename...Ts>
	struct reachable_symbols2<Rule<N, H, Ts...>, Visited>
	{
		typedef typename reachable_symbols<H,Visited>::type t;
		typedef typename reachable_symbols2<Rule<N, Ts...>, t>::type type;
	};

	template<typename Visited>
	struct reachable_symbols2<Rules<>, Visited>
	{
		typedef Visited type;
	};

	template<typename H, typename Visited, typename...Ts>
	struct reachable_symbols2<Rules<H, Ts...>, Visited>
	{
		typedef typename reachable_symbols2<H, Visited>::type t1;
		typedef typename reachable_symbols2<Rules<Ts...>, t1>::type type;
	};

	template<int N, typename T, typename Visited>
	struct reachable_symbols<Token<N, T>, Visited, false>
	{
		typedef typename ts_insert<Token<N, T>, Visited>::type type;
	};

	struct is_terminal
	{
		template<typename S>
		struct predicate
		{
			static const bool value = false;
		};

		template<int N, typename T>
		struct predicate<Token<N, T>>
		{
			static const bool value = true;
		};
	};

	template<typename S>
	struct reachable_terminals
	{
		typedef typename reachable_symbols<S>::type t;
		typedef typename ts_where<t, is_terminal>::type type;
	};

	template<typename S>
	struct reachable_nonterminals
	{
		typedef typename reachable_symbols<S>::type t;
		typedef typename ts_except<t, is_terminal>::type type;
	};


	typedef Token<-1, void> eof;

	template<typename Symbol>
	struct parser_construction
	{
		struct start
		{
			typedef Rule<-1, Symbol, eof> rule;
		};

		using symbols = typename reachable_symbols<start>::type;
		using terminals = typename ts_where<symbols, is_terminal>::type;
		using nonterminals = typename ts_except<symbols, is_terminal>::type;
	};
}
