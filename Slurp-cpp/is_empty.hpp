/*
	Computes whether a symbol (defined directly as a typedef/typename, or via a class) can be empty.

	bool is_empty<S>::value

	The complication is that this is a recursive predicate, and if you try to ask is_empty<s>::value
	whilst trying to compute is_empty<s>::value, then the C++ compiler will give an compilation error.

	Example:

	class e
	{
	  typename rule = Rules<
		e,
		Rule<100, e>,
		Rule<101, e, t>,
		Rule<102, t, e>,
		Rule<103>,
		Rule<104, r>,
		>;
	};

	In order to process this, we can just skip all recursive calls.
*/

namespace slurp
{

	template<typename T, typename Visited = ts_empty, bool Recursive = ts_contains<T, Visited>::value>
	struct is_empty
	{
		using visited = typename ts_concat<T, Visited>::type;
		static const bool value = is_empty<typename T::rule, visited>::value;
	};

	template<typename T, typename Visited>
	struct is_empty<T, Visited, true>
	{
		// Recursive case
		static const bool value = false;
		using visited = Visited;
	};

	template<int N, typename T, typename Visited>
	struct is_empty<Token<N, T>, Visited, false>
	{
		static const bool value = false;
		using visited = Visited;
	};

	template<int N, typename Visited>
	struct is_empty<Ch<N>, Visited, false>
	{
		static const bool value = false;
		using visited = Visited;
	};

	template<int A, int B, typename Visited>
	struct is_empty<Range<A, B>, Visited, false>
	{
		static const bool value = false;
		using visited = Visited;
	};

	template<typename A, typename... B, typename Visited>
	struct is_empty<Rules<A, B...>, Visited, false>
	{
		typedef is_empty<A, Visited> t1;
		typedef is_empty<Rules<B...>, Visited> t2;
		static const bool value = t1::value || t2::value;
		using visited = Visited;
	};

	template<typename Visited>
	struct is_empty<Rules<>, Visited, false>
	{
		static const bool value = false;
		using visited = Visited;
	};

	template<int N, typename Visited>
	struct is_empty<Rule<N>, Visited, false>
	{
		static const bool value = true;
		using visited = Visited;
	};

	template<int N, typename T, typename...Ts, typename Visited>
	struct is_empty<Rule<N, T, Ts...>, Visited, false>
	{
		using t1 = is_empty<T, Visited>;
		using t2 = is_empty<Rule<N, Ts...>, Visited>;
		using visited = Visited;

		static const bool value = t1::value && t2::value;
	};
}
