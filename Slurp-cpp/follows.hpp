namespace slurp
{
	template<typename R, typename S>
	struct follows
	{
		typedef typename follows<typename R::rule, S>::type type;
	};

	template<typename S>
	struct follows<Rules<>, S >
	{
		typedef ts_empty type;
	};
};