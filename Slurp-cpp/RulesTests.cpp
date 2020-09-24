
#include "Rules.hpp"
#include <type_traits>

#include "typeset.h"


using namespace slurp;

class Expr{};

static_assert(is_token<Ch<1>>::value, "");

static_assert(!is_token<Expr>::value, "");

static_assert(std::is_same<int, int>::value, "");

static_assert(std::is_same<ts_empty, ts_empty>::value, "");

static_assert(!ts_contains<int, ts_empty>::value, "");

static_assert(ts_contains<int, ts_insert<int, ts_empty>::type>::value, "");

typedef ts_insert<int, ts_empty>::type i;
typedef ts_insert<int, i>::type i;

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
	typedef Rule<11> rules;
};

struct Epsilon2
{
	typedef Rules<Epsilon1> rules;
};

struct Epsilon3
{
	typedef Rules<tok1, Epsilon1> rules;
};

struct Epsilon4
{
	typedef Rule<1, Epsilon3, Epsilon2, Epsilon1> rules;
};

static_assert(is_empty<Epsilon1>::value, "");
static_assert(is_empty<Epsilon2>::value, "");
static_assert(is_empty<Epsilon3>::value, "");
static_assert(is_empty<Epsilon4>::value, "");
static_assert(!is_empty<Rule<2, Epsilon2, tok1>>::value, "");
static_assert(!is_empty<Rule<2, tok1,Epsilon2>>::value, "");

// Test first


typedef typeset<tok1> first1;
typedef first<tok1>::type first1;


