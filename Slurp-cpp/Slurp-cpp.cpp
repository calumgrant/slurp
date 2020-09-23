// Slurp-cpp.cpp : Defines the entry point for the application.
//

#include "Slurp-cpp.h"
#include <vector>
#include <cassert>

#include "Stack.hpp"

namespace slurp
{


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

	class Parse
	{
	public:
		Parse(const char* string);

		const Node& root();
	};

	template<int ch> class Ch
	{ };

	template<int C1, int C2> class Range;

	template<typename ... S > class Seq;  // An unnamed rule: used 
}

namespace Example
{
	using namespace slurp;

	// To construct a parse tree, label each node in the parse tree with a kind.
	// Tokens are also placed in the parse tree, and share the same id-space
	// as parse nodes.
	enum nodes
	{
		Int, Open, Close, Plus, Minus, Times, Divide, Bracket
	};

	// Define the tokenizer

	typedef Range<'0', '9'> Digit;

	// An integer is an example of a recursive rule.
	class Integer
	{
		typedef Rules<
			Digit, 
			Seq<Integer, Digit>
		> rule;
	};

	class Expression;

	typedef Rules<
		Token<Int, Integer>,
		Rule<Bracket, Ch<'('>, Expression, Ch<')'>>
		>
		PrimaryExpr;

	class MultiplicativeExpr
	{
		typedef Rules<
			PrimaryExpr,
			Rule<Times, MultiplicativeExpr, Ch<'*'>, PrimaryExpr>,
			Rule<Divide, MultiplicativeExpr, Ch<'/'>, PrimaryExpr>
		> rule;
	};

	class AdditiveExpr
	{
		typedef Rules<
			MultiplicativeExpr,
			Rule<Plus, MultiplicativeExpr, Ch<'+'>, AdditiveExpr>,
			Rule<Minus, MultiplicativeExpr, Ch<'-'>, AdditiveExpr>
		> rule;
	};

	class Expression
	{
		typedef AdditiveExpr rule;
	};

	void testIt()
	{
	}
}

void TestStack()
{
	using namespace slurp;
	Stack stack;
	TokenData d;

	stack.Shift(1, d, "hello", 5);

	{
		const Node& top = stack.Root();
		assert(top.Kind == 1);
		assert(top.IsToken());
		assert(top.TextLength() == 5);
		assert(strcmp(top.Text(), "hello") == 0);
	}

	stack.Reduce(2, 1);

	{
		const Node& top = stack.Root();
		assert(!top.IsToken());
		assert(top.Kind == 2);
		assert(top.size() == 1);

		const Node& c = top[0];
		assert(c.IsToken());
		assert(c.Kind == 1);
		assert(strcmp(c.Text(), "hello") == 0);
	}

	stack.Shift(2, d, "World!", 6);

	stack.Shift(3, d, "World!", 6);
	stack.Reduce(10, 3);

	{
		auto& top = stack.Root();
		assert(top.size() == 3);
		assert(top[0].Kind == 2);
		assert(top[1].Kind == 2);
		assert(top[2].Kind == 3);
	}
}

int main()
{
	TestStack();
	std::cout << "Hello CMake." << std::endl;
	return 0;
}
