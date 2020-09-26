
namespace slurp
{
	template<typename Rule, int position, typename Lookahead>
	struct item;


	template<int position, typename Lookahead, typename...Ts>
	struct follows2;

	// For an item, compute the set of tokens that may follow
	// need to take into consideration empty symbols
	// If there is no follows, use the lookahead symbol of the item itelf
	template<typename Item>
	struct follows;

	template<int Position, int N, typename ... Ts, typename Lookahead>
	struct follows<item<Rule<N, Ts...>, Position, Lookahead>>
	{
		typedef typename follows2<Position, Lookahead, Ts...>::type type;
	};

	template<typename Lookahead>
	struct follows2<0, Lookahead>
	{
		typedef typeset<Lookahead> type;
	};

	template<int N, typename Lookahead, typename T, typename...Ts>
	struct follows2<N, Lookahead, T, Ts...>
	{
		typedef typename follows2<N - 1, Lookahead, Ts...>::type type;
	};

	template<typename Lookahead, typename T, typename...Ts>
	struct follows2<0, Lookahead, T, Ts...>
	{
		typedef typename first<T>::type t1;
		// typedef t1 type;
		// typename typename ts_union<t1, typename follows2<0, Lookahead, Ts...>::type>::type t2;
		typedef typename std::conditional<
			is_empty<T>::value,
			typename ts_union<t1, typename follows2<0, Lookahead, Ts...>::type>::type,
			t1>::type type;
	};

	// Produces a (possible empty) set of new items based on expanding a single given item.
	template<typename Item>
	struct expand;

	template<int Position, typename Lookahead, typename... Ts>
	struct expand2;

	template<int N, int Position, typename Lookahead, typename...Ts>
	struct expand<item<Rule<N, Ts...>, Position, Lookahead>>
	{
		typedef typename expand2<Position, Lookahead, Ts...>::type type;
	};

	template<int Position, typename T, typename... Ts, typename Lookahead>
	struct expand2<Position, Lookahead, T, Ts...>
	{
		typedef typename expand2<Position - 1, Lookahead, Ts...>::type type;
	};


	template<typename Lookahead>
	struct expand2<0, Lookahead>
	{
		typedef ts_empty type; // End of item -> No items to add.
	};

	// Expand all of the items in the pointed-at rule
	template<typename Lookaheads, typename T>
	struct expand3
	{
		typedef typename expand3<Lookaheads, typename T::rule>::type type;
	};


	// Expand a rule into items
	template<typename Lookaheads, typename Rule>
	struct expand4;

	template<typename Rule>
	struct expand4<ts_empty, Rule>
	{
		typedef ts_empty type;
	};

	template<typename T, typename...Ts, typename Rule>
	struct expand4<typeset<T, Ts...>, Rule>
	{
		typedef item<Rule, 0, T> t1;
		typedef typename expand4<typeset<Ts...>, Rule>::type t2;
		typedef typename ts_insert<t1, t2>::type type;
	};

	template<typename Lookahead, int N, typename... Ts>
	struct expand3<Lookahead, Rule<N, Ts...>>
	{
		typedef typename expand4<Lookahead, Rule<N, Ts...>>::type type;
	};

	template<typename Lookahead>
	struct expand3<Lookahead, Rules<>>
	{
		typedef ts_empty type;
	};

	template<typename Lookahead, typename T, typename...Ts>
	struct expand3<Lookahead, Rules<T, Ts...>>
	{
		typedef typename expand3<Lookahead, T>::type t1;
		typedef typename expand3<Lookahead, Rules<Ts...>>::type t2;
		typedef typename ts_union<t1, t2>::type type;
	};


	template<typename Lookahead, typename T, typename...Ts>
	struct expand2<0, Lookahead, T, Ts...>
	{
		typedef typename follows2<0, Lookahead, Ts...>::type lookaheads;

		typedef typename expand3<lookaheads, T>::type type;
		/*
			T is either a class, a Rule<> or Rules<> containing rules.
			For all rules in T, construct an item, using the lookahead.

		*/
	};


	template<typename Items>
	struct closure
	{
	};
}