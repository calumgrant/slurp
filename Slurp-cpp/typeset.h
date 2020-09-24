/*
The empty type set:
 
ts_empty

Inserting into a type set

ts_insert<Ts, T>::type

Test for inclusion:

ts_contains<Ts, T>::value

Test for subset:

ts_subset<T1, T2>::value;

Test for equality:

ts_equal<T1,T2>::value;
*/

#pragma once

namespace slurp
{
	template<typename... Ts>
	class typeset {};

	typedef typeset<> ts_empty;

	template<typename T, typename Ts>
	struct ts_insert;

	template<typename T>
	struct ts_insert<T, ts_empty>
	{
		typedef typeset<T> type;
	};

	template<typename H, typename...Ts>
	struct ts_insert<H, typeset<H, Ts...>>
	{
		typedef typeset<H, Ts...> type;
	};

	template<typename A, typename Ts>
	struct ts_concat;

	template<typename A>
	struct ts_concat<A, ts_empty>
	{
		typedef typeset<A> type;
	};

	template<typename A, typename H, typename... Ts>
	struct ts_concat<A, typeset<H, Ts...>>
	{
		typedef typeset<A, H, Ts...> type;
	};

	template<typename A, typename H, typename...Ts>
	struct ts_insert<A, typeset<H, Ts...>>
	{
		typedef typename ts_insert<A, typeset<Ts...>>::type tail;
		typedef typename ts_concat<H, tail>::type type;
	};

	template<typename T, typename Ts>
	struct ts_contains;

	template<typename T>
	struct ts_contains<T, ts_empty>
	{
		static const bool value = false;
	};

	template<typename H, typename...Ts>
	struct ts_contains<H, typeset<H, Ts...>>
	{
		static const bool value = true;
	};

	template<typename A, typename H, typename...Ts>
	struct ts_contains<A, typeset<H, Ts...>>
	{
		static const bool value = ts_contains<A, typeset<Ts...>>::value;
	};
}
