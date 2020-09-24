namespace slurp
{
	template<typename T> struct is_empty
	{
		static const bool value = is_empty<typename T::rules>::value;
	};

	template<int N, typename T> struct is_empty<Token<N, T>>
	{
		static const bool value = false;
	};

	template<int N> struct is_empty<Ch<N>>
	{
		static const bool value = false;
	};

	template<int A, int B> struct is_empty<Range<A, B>>
	{
		static const bool value = false;
	};

	template<typename A, typename... B>
	struct is_empty<Rules<A, B...>>
	{
		static const bool value = is_empty<A>::value || is_empty<Rules<B...>>::value;
	};

	template<>
	struct is_empty<Rules<>>
	{
		static const bool value = false;
	};

	template<int N>
	struct is_empty<Rule<N>>
	{
		static const bool value = true;
	};

	template<int N, typename T, typename...Ts>
	struct is_empty < Rule<N, T, Ts...>>
	{
		static const bool value = is_empty<T>::value && is_empty<Rule<N, Ts...>>::value;
	};
}
