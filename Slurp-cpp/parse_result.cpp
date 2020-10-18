#include "slurp.hpp"

slurp::parse_result::~parse_result()
{
}

slurp::parse_result::parse_result()
{
}

slurp::parse_result::parse_result(Stack&& stack) : stack(stack)
{
}

slurp::parse_result::operator bool() const
{
	return !stack.Empty();
}

void slurp::parse_result::DumpTree() const
{
	stack.DumpTree();
}

const slurp::Node& slurp::parse_result::root() const
{
	return stack.Root();
}