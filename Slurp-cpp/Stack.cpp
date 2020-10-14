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

void slurp::Stack::Shift(short kind, const TokenData& td, const char* text, unsigned length)
{
	unsigned newSize = length + sizeof(TokenData) + sizeof(Node) + 1;
	data.reserve(data.size() + newSize);

	Append(&td, sizeof(TokenData));
	Append(text, length);
	data.push_back(0);
	Node node(kind, 0, newSize);
	Append(&node, sizeof(Node));
}

inline void slurp::Stack::Append(const void* d, size_type s)
{
	data.insert(data.end(), (const char*)d, (const char*)d + s);
}

inline void slurp::Stack::Append(size_type s)
{
	data.resize(data.size() + s);
}

void slurp::Stack::Shift(short kind, const TokenData& td, unsigned length)
{
	unsigned newSize = length + sizeof(TokenData) + sizeof(Node) + 1;
	data.reserve(data.size() + newSize);

	Append(&td, sizeof(TokenData));
	Append(length);
	data.push_back(0);
	Node node(kind, 0, newSize);
	Append(&node, sizeof(Node));
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
		std::cout << node.Kind << ": " << node.Text() << std::endl;
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
