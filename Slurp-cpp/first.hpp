#include <type_traits>

namespace slurp
{
	template<typename T, typename Visited = ts_empty, bool Recursive = ts_contains<T,Visited>::value>
	struct first
	{
		typedef typename ts_concat<T, Visited>::type visited;
		typedef typename first<typename T::rule, visited>::type type;
	};

	template<typename T, typename Visited>
	struct first<T, Visited, true>
	{
		typedef ts_empty type;
		typedef Visited visited;
	};


	template<int C, typename Visited>
	struct first<Ch<C>, Visited, false>
	{
		typedef typeset<Ch<C>> type;
		typedef Visited visited;
	};

	template<int N, typename T, typename Visited>
	struct first<Token<N, T>, Visited, false>
	{
		typedef Visited visited;
		typedef typeset<Token<N, T>> type;
	};

	template<typename Visited>
	struct first<Rules<>, Visited, false>
	{
		typedef ts_empty type;
		typedef Visited visited;
	};

	template<typename H, typename... T, typename Visited>
	struct first<Rules<H, T...>, Visited, false>
	{
		typedef typename first<H, Visited>::type t1;
		typedef typename first<Rules<T...>, Visited>::type t2;
		typedef typename ts_union<t1, t2>::type type;
		typedef Visited visited;
	};

	template<int N, typename Visited>
	struct first<Rule<N>, Visited, false>
	{
		typedef ts_empty type;
		typedef Visited visited;
	};

	template<int N, typename H, typename...T, typename Visited>
	struct first<Rule<N, H, T...>, Visited, false>
	{
		typedef typename first<H, Visited>::type t1;
		typedef typename first<Rule<N, T...>, Visited>::type t2;
		typedef typename std::conditional<
			is_empty<H>::value,
			typename ts_union<t1, t2>::type,
			t1>::type type;
		typedef Visited visited;
	};
}
