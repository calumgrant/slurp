
#include "slurp.hpp"
#include <type_traits>



using namespace slurp;

class Expr{};

static_assert(is_token<Ch<1>>::value, "");

static_assert(!is_token<Expr>::value, "");

static_assert(std::is_same<int, int>::value, "");

static_assert(std::is_same<ts_empty, ts_empty>::value, "");


static_assert(!is_empty<Ch<0>>::value, "");
static_assert(!is_empty<Range<0,10>>::value, "");
static_assert(!is_empty<Token<123,Ch<0>>>::value, "");

static_assert(is_empty<Rule<12>>::value, "");

static_assert(is_empty<Rules<Rule<12>>>::value, "");

typedef Token<1, Ch<'0'>> tok1;

static_assert(!is_empty<Rules<Rule<12, tok1>>>::value, "");
static_assert(!is_empty<Rules<Rule<12, tok1>>>::value, "");

struct Epsilon1
{
	typedef Rule<11> rule;
};

struct Epsilon2
{
	typedef Rules<Epsilon1> rule;
};

struct Epsilon3
{
	typedef Rules<tok1, Epsilon1> rule;
};

struct Epsilon4
{
	typedef Rule<1, Epsilon1> rule;
};


struct Epsilon5
{
	typedef Rule<1, Epsilon1, Epsilon2/*, Epsilon1*/> rule;
};

static_assert(is_empty<Epsilon1>::value, "");
static_assert(is_empty<Epsilon2>::value, "");
static_assert(is_empty<Epsilon3>::value, "");
static_assert(is_empty<Epsilon4>::value, "");

static_assert(is_empty<Rule<1>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Rule<2>>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Rule<2>, Rule<3>>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Rule<1>, Rule<1>>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Epsilon1>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Epsilon1, Epsilon1>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Rule<2>, Rule<3>>>::value, "Symbol should be is_empty");
static_assert(is_empty<Rule<1, Epsilon1, Epsilon2>>::value, "Symbol should be is_empty");

static_assert(is_empty<Epsilon5>::value, "Symbol should be is_empty");
static_assert(!is_empty<Rule<2, Epsilon2, tok1>>::value, "");
static_assert(!is_empty<Rule<2, tok1,Epsilon2>>::value, "");

// Test first


typedef typeset<tok1> first1;
typedef first<tok1>::type first1;
typedef first<Rules<tok1, tok1>>::type first1;


namespace ExprTests
{
	typedef Token<'(', Ch<'('>> tok_open;
	typedef Token<')', Ch<')'>> tok_close;
	typedef Token<123, Range<'0', '9'>> tok_int;

	struct Expr
	{
		typedef Rules <
			Rule<1, tok_open, Expr, tok_close>,
			Rule<2, tok_int>
		> rule;
	};

	typedef reachable_symbols<Expr>::type Expr_reachable;
	static_assert(ts_contains<Expr, Expr_reachable>::value, "");
	static_assert(ts_contains<tok_open, Expr_reachable>::value, "");
	static_assert(ts_contains<tok_close, Expr_reachable>::value, "");
	static_assert(ts_contains<tok_int, Expr_reachable>::value, "");
	static_assert(!ts_contains<Expr::rule, Expr_reachable>::value, "");
	static_assert(ts_size<Expr_reachable>::value == 4, "");

	typedef parser_construction<Expr> parser1;
	static_assert(ts_contains<Expr, parser1::symbols>::value, "");
	static_assert(ts_contains<eof, parser1::symbols>::value, "");
	static_assert(ts_contains<eof, parser1::terminals>::value, "");
	static_assert(ts_contains<Expr, parser1::nonterminals>::value, "");

	// start, eof, Expr, int, open, close
	static_assert(ts_size<parser1::symbols>::value == 6, "");
	static_assert(ts_size<parser1::terminals>::value == 4, "");
	static_assert(ts_size<parser1::nonterminals>::value == 2, "Unexpected number of nonterminals");

	// Check the first
	// Ensure that first/is-empty work forrecursive predicates.
	static_assert(!is_empty<Expr>::value, "");
	typedef first<Expr>::type e_first;

	// Currently fails because first doesn't handle recursive rules (bug)
	static_assert(ts_size<e_first>::value == 2, "");
}

namespace RecursiveRules
{
	enum tokens { a, b };

	typedef Token<a, Ch<'a'>> tok_a;
	typedef Token<b, Ch<'b'>> tok_b;

	typedef Rules<
		Rule<b>,
		tok_a
		> MaybeA;

	struct Expr
	{
		typedef Rules<
			Rule<1, MaybeA, Expr>,
			Rule<2, MaybeA>
			> rule;
	};

	static_assert(is_empty<MaybeA>::value, "");
	static_assert(ts_contains<tok_a, first<MaybeA>::type>::value, "");

	static_assert(is_empty<Expr>::value, "");

	// Fix recursive first()
	static_assert(ts_contains<tok_a, first<Expr>::type>::value, "");

	static_assert(ts_contains<tok_a, first<tok_a>::type > ::value, "");

	struct A
	{
		typedef Rules<
			Rule<1, MaybeA, A>,
			Rule<2, tok_a, tok_a, A>
		> rule;
	};

	struct B
	{
		typedef Rules<
			Rule<3, tok_b, A, B>
		> rule;
	};

	static_assert(ts_contains<tok_b, first<B>::type>::value, "");
	static_assert(ts_contains<tok_a, first<A>::type>::value, "");

	struct C
	{
		typedef Rules<
			Rule<3, MaybeA, tok_b, B>
		> rule;
	};
	static_assert(ts_contains<tok_a, first<C>::type>::value, "");
	static_assert(ts_contains<tok_b, first<C>::type>::value, "");

	typedef item<Rule<100, C>, 0, tok_b> item1;

	typedef follows<item1>::type follow1;
	static_assert(ts_contains<tok_a, follow1>::value, "");
	static_assert(ts_contains<tok_b, follow1>::value, "");

	// typedef 

	// Expansion

	typedef expand<item1>::type t1;

	// Let's print this then.
}

namespace ExpandTests
{

}

namespace RecusiveTests
{
	struct Expr
	{
		typedef Rules<
			Token<123, Ch<'x'>>
		> rule;
	};

	struct Expr2
	{
		typedef Expr2 rule;
	};

	struct Expr3
	{
		typedef Rules<Expr3> rule;
	};

	struct Expr4
	{
		typedef Rule<123, Expr4> rule;
	};

	struct Expr5
	{
		typedef Rule<123, Rule<124>, Expr5> rule;
	};

	struct Expr6
	{
		typedef Rule<123, Token<124, Ch<'x'>>, Expr6> rule;
	};


	static_assert(!front_recursive<Expr>::value, "Expr is front-recursive");
	static_assert(front_recursive<Expr2>::value, "Expr is front-recursive");
	static_assert(front_recursive<Expr3>::value, "Expr is front-recursive");
	static_assert(front_recursive<Expr4>::value, "Expr is front-recursive");
	static_assert(front_recursive<Expr5>::value, "Expr is front-recursive");
	static_assert(!front_recursive<Expr6>::value, "Expr is front-recursive");
};
