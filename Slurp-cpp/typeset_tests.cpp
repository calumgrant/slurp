#include "typeset.h"

using namespace slurp;

static_assert(!ts_contains<int, ts_empty>::value, "");

static_assert(ts_contains<int, ts_insert<int, ts_empty>::type>::value, "");

namespace
{
	typedef typeset<int, float> ts1;
	typedef typeset<float, int> ts2;
	typedef ts_union<ts1, ts1>::type ts1;

	static_assert(ts_subset<ts_empty, ts1>::value, "");
	static_assert(ts_equal<ts1, ts2>::value, "");
	static_assert(ts_equal<ts1, ts1>::value, "");
	static_assert(ts_equal<ts2, ts2>::value, "");
	static_assert(ts_equal<ts2, ts1>::value, "");
	static_assert(!ts_equal<ts_empty, ts1>::value, "");
}

typedef ts_insert<int, ts_empty>::type i;
typedef ts_insert<int, i>::type i;

