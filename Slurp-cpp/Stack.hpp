#include "Node.h"
#include <vector>

namespace slurp
{
	/*
		An efficient data structure for storing an abstract syntax tree (AST).

		It makes it extremely efficient to construct the tree, as it is just bytes
		in a vector, and makes it extremely efficient to construct the parse tree
		because all data is just pushed to the vector.

		Because pointers are not stored explicitly in Nodes, there is no need
		to move pointers when the vector resizes. Pointers to nodes can become invalid when the vector
		resizes but remain stable once the parse tree has been constructed.

		The root of the stack is always stored at the end of the stack,
		so we do not need to store a pointer to the root of the tree.

		The basic operations of an LR parser are efficient to implement:
		- Shift appends a token node to the end of the stack.
		- Reduce appends a node to the end of the stack.
		Neither operation requires data to be moved within the stack.
	*/
	class Stack
	{

	public:
		Stack();
		~Stack();

		/*
			Gets the root of the parse tree.
			If the parse tree is empty then this is undefined.
		*/
		const Node& Root() const;

		Node& Root();

		/*
			Reduces the last n nodes on the stack into a single node.
			The new node has kind "kind".
			Ensure that there are enough nodes on the stack prior to this call
			otherwise the result is undefined.
		*/
		void Reduce(short kind, unsigned short numberOfChildren);

		/*
			Shifts (pushes) a new token node onto the stack.
		*/
		void Shift(short kind, const TokenData& data, const char* text, unsigned textLength);

		// ?? How to convert non-ascii tokens??
		template<typename It>
		void Shift(short kind, const TokenData& data, It start, It end)
		{
			// !! Check overflows
			Shift(kind, data, (unsigned)(end - start));  // !! copy thee data
		}

		void Shift(short kind, const TokenData& data, unsigned length);

		void DumpTree() const;
		typedef unsigned size_type;

		size_type Top() const;

		// Unwinds the stack to a position previously given by Top();
		void Unwind(size_type position);

	private:
		void Append(const void* src, size_type length);
		void Append(size_type length);
		static void DumpTree(const Node& node, int indent);

		std::vector<char> data;
	};
}
