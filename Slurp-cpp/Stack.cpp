#include "Stack.hpp"
#include <iostream>

slurp::Stack::Stack()
{
	data.reserve(2048);
}

slurp::Stack::~Stack()
{
}

const slurp::Node& slurp::Stack::Root() const
{
	return *((const Node*)((const char*)&data[0] + data.size()) - 1);
}

slurp::Node& slurp::Stack::Root()
{
	return *((Node*)((char*)&data[0] + data.size()) - 1);
}

inline void slurp::Stack::Append(const void* d, size_type s)
{
	data.insert(data.end(), (const char*)d, (const char*)d + s);
}

inline void slurp::Stack::Append(size_type s)
{
	data.resize(data.size() + s);
}

wchar_t *slurp::Stack::Shift(short kind, const TokenData& td, unsigned length)
{
	unsigned newSize = (length+1)*sizeof(wchar_t) + sizeof(TokenData) + sizeof(Node);
	data.reserve(data.size() + newSize);

	Append(&td, sizeof(TokenData));
	auto pos = data.size();
	Append((length+1)*sizeof(wchar_t));
	Node node(kind, 0, newSize);
	Append(&node, sizeof(Node));
	return (wchar_t*)(&data[pos]);
}


void slurp::Stack::Reduce(short kind, unsigned short numberOfChildren)
{
	assert(numberOfChildren > 0);

	size_type totalSize = sizeof(Node);
	const Node* child = &Root();

	for (int i = 0; i < numberOfChildren;
		++i, child = child->NextChild())
	{
		totalSize += child->length;
	}

	Node node(kind, numberOfChildren, totalSize);
	Append(&node, sizeof(Node));
}

void slurp::Stack::DumpTree() const
{
	if (data.empty())
	{
		std::cout << "<empty>\n";
		return;
	}

	DumpTree(Root(), 0);
}

void slurp::Stack::DumpTree(const Node& node, int indent)
{
	if (node.IsToken())
	{
		for (int i = 0; i < indent; ++i) std::cout << ' ';
		std::wcout << node.Kind << ": " << node.WText() << std::endl;
	}
	else
	{
		for (int i = 0; i < indent; ++i) std::cout << ' ';
		std::cout << node.Kind << ":" << std::endl;
		for (int i = 0; i < node.size(); ++i)
			DumpTree(node[i], indent + 2);
	}
}

unsigned slurp::Stack::Top() const
{
	return (unsigned)data.size();
}

void slurp::Stack::Unwind(unsigned size)
{
	data.resize(size);
}

bool slurp::Stack::Empty() const
{
	return data.empty();
}
