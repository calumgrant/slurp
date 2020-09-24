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
	// Matches a single character.
	template<int ch> class Ch;

	// Matches a single character in a range.
	template<int C1, int C2> class Range;

	// Matches a sequence.
	template<typename ... S > class Seq;  // An unnamed rule: used to build tokens

	// A Token symbol - a terminal symbol.
	// When this is matched, it creates a token node in the parse tree.
	template<int Kind, typename Rule> class Token;

	// A rule symbol, consistint of a sequence of other symbols.
	// The rule may be empty.
	// When this is matched, it creates a node with the given number of children in the parse tree. 
	template<int Kind, typename ... Symbols> class Rule;

	// A list of rules, used to define either
	// a token or a symbol.
	template<typename ... Rs> class Rules;

	// A sequence of rules, used to define a token.
	template<typename ... Rs> class Seq;

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

	// 
	// Compute first and follows





}
