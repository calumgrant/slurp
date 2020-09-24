#pragma once
#include <cassert>

namespace slurp
{
	struct TokenData
	{
		unsigned offset, row, column;
	};


	/*
		A node in the parse tree.
		Nodes are designed to be placed on a stack (class slurp::Stack)
		and locate their children by looking up the stack relative
		to the "this" pointer.

		The children of a node immediately preceed "this" on the stack.
		The first child of a node appears immediately prior to "this"
		node and is located at "this - 8 bytes" on the stack.
		The next child of a node is located at "this - length" on the stack.

		It is not possible to nevigate to the parent of a node without storing this as
		an extra field.

		This means that the node does not need to store a pointer to its children,
		which saves space.

		The "root" of the stack is always the top node on the stack.

		The layout of a node is as follows

		Child n-2 (at this - 8 - length of child n - legnth
		Child n-1 (at this - 8 - length of child n)
		Child n (at this-8)
		Node

		The layout of a token is as follows:

		0: TokenData (8 bytes, at this - length + sizeof Node)
		8: Text (n bytes)
		n: Node

		The layout of a token (without text content) is as follows:
		Node (8 bytes)

		This scheme makes it very efficient and an LR parser to enerate the AST as the parser
		is effectively writing to the end of a vector<char> at all times.
	*/
	class Node
	{
	private:
		friend class Stack;

		unsigned length; // The total length of this node in bytes

		unsigned short numberOfChildren;
	public:
		typedef unsigned size_type;

		Node(short kind, unsigned short children, size_type length) :
			length(length), Kind(kind), numberOfChildren(children)
		{
		}

		unsigned short size() const { return numberOfChildren; }

		const Node& operator[](unsigned short index) const
		{
			assert(index < numberOfChildren);

			const Node* c = FirstChild();
			for (int i = index+1; i < numberOfChildren; ++i)
				c = c->NextChild();
			return *c;
		}

		short Kind;

		const Node* NextChild() const
		{
			return (const Node*)((const char*)this - length);
		}

		Node* NextChild()
		{
			return (Node*)((char*)this - length);
		}

		const Node* FirstChild() const
		{
			return this - 1;
		}

		Node* FirstChild()
		{
			return this - 1;
		}

		bool IsToken() const { return numberOfChildren == 0; }

		// Gets the token data if this is a token (IsToken()==true)
		// Undefined if IsToken()==false
		const TokenData* GetToken() const
		{
			return (TokenData*)data();
		}

		typedef unsigned size_type;

		size_type TextLength() const { return IsToken() ? length - sizeof(Node) - sizeof(TokenData) -1 : 0; }

		const char* Text() const { return IsToken() ? (const char*)(GetToken() + 1) : ""; }

		// Size
	private:
		const void* data() const { return (const char*)(this) - length + sizeof(Node); }
	};

}