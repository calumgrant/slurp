#include <type_traits>

namespace slurp
{
	template<typename T>
	struct first
	{
		typedef typename first<typename T::rule>::type type;
	};

	template<int C>
	struct first<Ch<C>>
	{
		typedef typeset<Ch<C>> type;
	};

	template<int N, typename T>
	struct first<Token<N, T>>
	{
		typedef typeset<Token<N, T>> type;
	};

	template<>
	struct first<Rules<>>
	{
		typedef ts_empty type;
	};

	template<typename H, typename... T>
	struct first<Rules<H, T...>>
	{
		typedef typename first<H>::type t1;
		typedef typename first<Rules<T...>>::type t2;
		typedef typename ts_union<t1, t2>::type type;
	};

	template<int N>
	struct first<Rule<N>>
	{
		typedef ts_empty type;
	};

	template<int N, typename H, typename...T>
	struct first<Rule<N, H, T...>>
	{
		typedef typename first<H>::type t1;
		typedef typename first<Rule<N, T...>>::type t2;
		typedef typename std::conditional<
			is_empty<H>::value,
			typename ts_union<t1, t2>::type,
			t1>::type type;
	};
}
