// Slurp-cpp.cpp : Defines the entry point for the application.
//

#include "Slurp-cpp.h"
#include <vector>

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
		unsigned length; // The total length of this node in bytes

		unsigned short numberOfChildren;

	public:
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

		std::size_t TextLength() const { return length - sizeof(Node) - sizeof(TokenData); }

		const char* Text() const { return (const char*)GetToken() + 1; }

		// Iterators

		// Size
		const Node* GetChild(int child) const;
	private:
		const void* data() const { return (const char*)(this) - length - sizeof(Node); }
	};

	/*
		An efficient data structure for storing an abstract syntax tree (AST).

		It also makes it extremely efficient to construct the tree, as it's just bytes
		in a vector, and makes it extremely efficient to construct the parse tree
		because all data is just pushed to the vector.

		Because pointers are not stored explicitly in Nodes, there is no need
		to move pointers when the vector resizes. Pointers to nodes can become invalid when the vector
		resizes but remain stable once the parse tree has been constructed.

		The root of the stack is always stored at the top of the stack,
		so we do not need to store a pointer to the root of the tree. 

		The basic operations of an LR parser are efficient to implement:
		- Shift appends a token node to the end of the stack.
		- Reduce appends a node to the end of the stack.
		Neither operation requires data to be moved within the stack.
	*/
	class ParseStack
	{
		std::vector<char> data;

	public:

		const Node& Root() const { return *(const Node*)((const char*)&data[0] + data.size()) - 1; }

		/*
			Reduces the last n nodes on the stack into a single node.
			The new node has kind "kind".
			Ensure that there are enough nodes on the stack prior to this call
			otherwise the result is undefined.
		*/
		void Reduce(short kind, short numberOfChildren);

		/*
			Shifts (pushes) a new token node onto the stack.
		*/
		void Shift(short kind, const TokenData& data, const char* text, unsigned textLength);
	};

	template<int Kind, typename Rule>
	class Token
	{

	};

	template<int Kind, typename ... Symbols>
	class Rule
	{

	};

	template<typename ... Rs>
	class Rules {
	};

	class Parse
	{
	public:
		Parse(const char* string);

		const Node& root();
	};

	template<int ch> class Ch
	{ };

	template<int C1, int C2> class Range;

	template<typename ... S > class Seq;  // An unnamed rule: used 
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

	typedef Token<Int, Integer> IntToken

	//

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
		slurp::Parse p("123");

		// Navigating the parse tree
		const Example::Node& root = p.root();

		// Probe what we have
		if (root.Kind == Plus)
		{

		}
	}
}

void TestTrees()
{
	slurp::Stack stack;

}

int main()
{
	std::cout << "Hello CMake." << std::endl;
	return 0;
}
