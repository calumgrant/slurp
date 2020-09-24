/*
	Classes for defining a grammar.

	There are three types of class:
	1) A token-part, such as Ch<>, Range<> and Seq<>
	1a) A class representing a token-part
	2) A token, Token<int, token_part>
	3) A symbol, Rule<> or Rules<>
	3a) A class representing a symbol

	Token-parts are used 
*/

#include "typeset.h"

namespace slurp
{
	template<int ch> class Ch
	{ };

	template<int C1, int C2> class Range;

	template<typename ... S > class Seq;  // An unnamed rule: used 

	template<int Kind, typename Rule>
	class Token
	{

	};

	template<int Kind, typename ... Symbols>
	class Rule
	{

	};

	template<typename ... Rs>
	class Rules {
	};

	template<typename ... Rs>
	class Seq
	{

	};

	template<typename T>
	struct is_token
	{
		static const bool value = false;
	};

	template<int C>
	struct is_token<Ch<C>>
	{
		static const bool value = true;
	};

	template<typename T>
	struct get_rules
	{
		typedef typename T::rule type;
	};

	template<typename S>
	struct maybe_empty
	{
		static const bool value = maybe_empty<typename S::rules>::value value;
	};

	template<typename T>
	struct single_char_token_part
	{
		static const bool value = false;
	};

	template<typename T>
	struct rule_traits : public rule_traits<typename T::rules>
	{
		typedef typename T::rules rules;
	};

	template<int N>
	struct rule_traits<Ch<N>>
	{
		static const bool is_token = true;
		static const bool single_character_token = true;
	};

	// 
	// Compute first and follows


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

	template<int A, int B> struct is_empty<Range<A,B>>
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

	template<typename T>
	struct first
	{
		typedef typename first<typename T::rules>::type type;
	};

	template<int C>
	struct first<Ch<C>>
	{
		typedef typeset<Ch<C>> type;
	};

	template<int N, typename T>
	struct first<Token<N,T>>
	{
		typedef typeset<Token<N, T>> type;
	};

	//template<int N, typename A>
	//struct first<


}
