// Slurp-cpp.cpp : Defines the entry point for the application.
//

#include "Slurp-cpp.h"

#include "slurp.hpp"
#include "prettyprint.hpp"

#include <sstream>

namespace slurp
{

	class Parse
	{
	public:
		Parse(const char* string);

		const Node& root();
	};

	template<typename... Tokens>
	class Tokenizer
	{
	};	
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
		// typedef Tokenizer < typeset<Token<1, Ch<'x'>>> tok2;

	}
}

void TestStack()
{
	using namespace slurp;
	Stack stack;
	TokenData d;

	char hello[] = "hello";
	stack.Shift(1, d, hello, hello + 5);

	{
		const Node& top = stack.Root();
		assert(top.Kind == 1);
		assert(top.IsToken());
		assert(top.WTextLength() == 5);
		assert(top.Str() == L"hello");
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
		assert(c.Str() == L"hello");
	}

	char world[] = "World!";
	stack.Shift(2, d, world, world+6);

	stack.Shift(3, d, world, world+6);
	stack.Reduce(10, 3);

	{
		auto& top = stack.Root();
		assert(top.size() == 3);
		assert(top[0].Kind == 2);
		assert(top[1].Kind == 2);
		assert(top[2].Kind == 3);
	}
}

struct Statement
{
};

void PrintStuff()
{
	using namespace slurp;
	enum { open, close, i, bracket };

	typedef Token<open, Ch<'('>> tok_open;
	typedef Token<close, Ch<')'>> tok_close;
	typedef Token<i, Range<'0', '9'>> tok_int;

	struct Expr
	{
		typedef Rules<
			Rule<bracket, tok_open, Expr, tok_close>,
			tok_int
		> rule;
	};

	struct Expr2
	{
		typedef Rules<
			Rule<bracket, tok_open, Expr2, tok_close>,
			Rule<123, tok_int>
		> rule;
	};


	std::cout <<
		print<typeset<>> << std::endl <<
		print<Token<123, Ch<'X'>>> << std::endl <<
		print<Expr> << std::endl <<
		print<Statement> << std::endl <<
		print<Rule<123, Expr, Statement>> << std::endl;

	std::cout << "Rules:\n" << print<Expr::rule> << std::endl;
	
	// Bug
	std::cout << "Reachable: " << print<reachable_symbols<Expr>::type> << std::endl;

	std::cout << "Rules:\n" << print<Expr2::rule> << std::endl;
	std::cout << "Reachable: " << print<reachable_symbols<Expr2>::type> << std::endl;
	typedef parser_construction<Expr> c1;
	std::cout << print<c1::terminals> << std::endl;
	std::cout << print<c1::nonterminals> << std::endl;
}

#include <stack>

namespace ManualTableExample
{
	// An complete example of a hand written LR parser.
	enum Actions { Error, Shift, Reduce, Accept, Goto };

	// Hard coded symbols. a,b,eof are terminals, and E is the non-terminal.
	enum Symbol { a, b, eof, E, NUMBER_OF_SYMBOLS };

	// An entry in the parser table.
	// For expedience, the non-terminal gotos are also encoded in the same table.
	struct Action {
		Actions action;
		union { int state, rule; };
	};

	// A row in the parser table.
	struct State
	{
		Action actions[NUMBER_OF_SYMBOLS];
	};

	// Information about rules, needed when the parser reduces.
	struct Rule
	{
		int length;
		Symbol symbol;
	};

	// The core parser algorithm of an LR parser
	// input: a sequence of tokens, that must be terminated by the eof symbol
	// states: the computed actions and goto table.
	// rules: The length and resultant symbol of each rule.
	// Returns true if parsing was successful.
	bool parse0(const Symbol input[], const State states[], const Rule rules[])
	{
		// In this example the stack only stores the state.
		// Real LR parsers would also store an additional value in the stack.
		std::stack<int> stack;
		int state = 0;
		for (; ; ++input)
		{
			// Reduce the stack 0 or more times for each input
			while (states[state].actions[*input].action == Reduce)
			{
				// Look up the rule that is being reduced.
				const Rule &rule = rules[states[state].actions[*input].rule];
				// Pop the correct number of symbols from the stack.
				// Real LR parsers would also report the position of the match
				// and compute the parse node at this point.
				for (int s = 1; s < rule.length; ++s)
					stack.pop();
				state = states[stack.top()].actions[rule.symbol].state;
			}
			switch (states[state].actions[*input].action)
			{
			case Error:
				return false;  // Syntax error
			case Shift:
				stack.push(state);
				state = states[state].actions[*input].state;
				break;
			case Accept:
				return true;  // Parse success
			}
		}
	}

	bool parse(const Symbol input[], const State states[], const Rule rules[])
	{
		std::stack<int> stack;
		stack.push(0);
		for (;; ++input)
		{
			while (states[stack.top()].actions[*input].action == Reduce)
			{
				const Rule& rule = rules[states[stack.top()].actions[*input].rule];
				for (int s = 0; s < rule.length; ++s)
					stack.pop();
				stack.push(states[stack.top()].actions[rule.symbol].state);
			}
			switch (states[stack.top()].actions[*input].action)
			{
			case Error:
				return false;
			case Shift:
				stack.push(states[stack.top()].actions[*input].state);
				break;
			case Accept:
				return true;
			}
		}
	}


	void examplelr()
	{
		// Manually computed parser table for the grammar
		// E -> a b
		// E -> a E b

		Rule rules[] = { Rule { 2, E }, Rule { 3, E } };

		Action error { Error, 0 };

		// The manually crafted parser table.
		State states[10] =
		{
			{ Action { Shift, 1 }, error, error, Action { Goto, 2} },
			{ Action { Shift, 3}, Action {Shift,5},error, Action { Goto,6}},
			{ error, error, Action{Accept}, error },
			{ Action { Shift, 3}, Action { Shift, 4}, error, Action { Goto, 7}},
			{ error, Action{ Reduce, 0 }, error, error },
			{ error, error, Action { Reduce, 0 }, error },
			{ error, Action { Shift, 8 }, error, error },
			{ error, Action { Shift, 9 }, error, error },
			{ error, error, Action { Reduce, 1 }, error },
			{ error, Action { Reduce, 1 }, error, error }
		};

		// Some sample programs
		Symbol program1[] = { eof };
		Symbol program2[] = { a, b, eof };
		Symbol program3[] = { a, a, a, b, b, b, eof };
		Symbol program4[] = { a, a, b, eof };
		Symbol program5[] = { a, a, b, b, b, eof };
		Symbol program6[] = { a, a, b, b, eof };
		Symbol* programs[] = { program1, program2, program3, program4, program5, program6 };

		for (int p = 0; p < 6; ++p)
		{
			std::cout << "Program " << p << " parse result = " << parse(programs[p], states, rules) << std::endl;
		}
	}
}

namespace RD
{
	using namespace slurp;

	typedef Token<'d', Ch<'d'>> Digit;

	struct Integer
	{
		typedef Rules<
			Digit,
			Rule<'i', Digit, Integer>
		> rule;
	};

	void TestRecursiveDescent()
	{
		null_tokenizer tok;

		char input[] = "ddx";
		auto p = recursive_descent<Integer>(tok, input, input + 2);
		assert(p);
		assert(p.root() == 'i');
		assert(p.root().size() == 2);
		assert(p.root()[0] == 'd');
		assert(p.root()[0] == 'd');

		// Check the token text in the result
		// Check the positional offsets in the text
		// TODO

		p = recursive_descent<Integer>(tok, input, input + 3);
		assert(!p);

		//Test a long string
		{
			std::stringstream ss;

			// Stack overflow problem!!
			for (int i = 0; i < 10000; ++i)
				ss << 'd';

			auto s = ss.str();
			p = recursive_descent<Integer>(tok, s.begin(), s.end());
			assert(p);
///			p.DumpTree();
		}

	}
}

struct Test
{
	typedef Test member;
};

template<typename T>
struct foo
{
	static void parse() {
		return foo<T::member>::parse();
	}
};

int main()
{

	// foo<Test>::parse();

	ManualTableExample::examplelr();
	TestStack();
	PrintStuff();
	RD::TestRecursiveDescent();
	std::cout << "Hello CMake." << std::endl;
	return 0;
}
