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

#include <type_traits>

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

	template<typename Ts1, typename Ts2>
	struct ts_union;

	template<typename Ts2>
	struct ts_union<ts_empty, Ts2>
	{
		typedef Ts2 type;
	};

	template<typename A, typename... Ts, typename Ts2>
	struct ts_union<typeset<A, Ts...>, Ts2>
	{
		typedef typename ts_union<typeset<Ts...>, Ts2>::type t1;
		typedef typename ts_insert<A, t1>::type type;
	};

	template<typename Ts1, typename Ts2>
	struct ts_subset;

	template<typename Ts2>
	struct ts_subset<ts_empty, Ts2>
	{
		static const bool value = true;
	};

	template<typename H, typename...Ts, typename Ts2>
	struct ts_subset<typeset<H, Ts...>, Ts2>
	{
		static const bool value = ts_contains<H, Ts2>::value && ts_subset<typeset<Ts...>, Ts2>::value;
	};

	template<typename Ts1, typename Ts2>
	struct ts_equal
	{
		static const bool value = ts_subset<Ts1, Ts2>::value && ts_subset<Ts2, Ts1>::value;
	};

	template<typename Ts>
	struct ts_size;

	template<>
	struct ts_size<typeset<>>
	{
		static const int value = 0;
	};

	template<typename H, typename...Ts>
	struct ts_size<typeset<H, Ts...>>
	{
		static const int value = 1 + ts_size<typeset<Ts...>>::value;
	};

	template<typename Ts, typename Predicate>
	struct ts_where;

	template<typename Predicate>
	struct ts_where<typeset<>, Predicate>
	{
		typedef typeset<> type;
	};

	template<typename H,typename...Ts, typename Predicate>
	struct ts_where<typeset<H, Ts...>, Predicate>
	{
		typedef typename ts_where<typeset<Ts...>, Predicate>::type t;

		typedef typename std::conditional<Predicate::template predicate<H>::value,
			typename ts_concat<H, t>::type,
			t>::type type;
	};

	template<typename Ts, typename Predicate>
	struct ts_except;

	template<typename Predicate>
	struct ts_except<typeset<>, Predicate>
	{
		typedef typeset<> type;
	};

	template<typename H, typename...Ts, typename Predicate>
	struct ts_except<typeset<H, Ts...>, Predicate>
	{
		typedef typename ts_except<typeset<Ts...>, Predicate>::type t;

		typedef typename std::conditional<Predicate::template predicate<H>::value,
			t,
			typename ts_concat<H, t>::type>::type type;
	};

}
