
namespace slurp
{
	class parse_error
	{
	};

	// Holds the result of a parse.
	// This is either a complete parse tree (stored in an efficient Stack data structure)
	// or an error indication.
	class parse_result
	{
	public:

		// true if the parse was successful or
		// false if there were syntax errors.
		operator bool() const;

		void DumpTree() const;

		// Gets the root of the parse tree.
		// Undefined if the parse has not completed successfully.
		const Node& root() const;

		// A list of syntax errors !!
		TokenData syntaxError;

		parse_result();

		// Constucts a parse result containing a successful parse tree
		parse_result(Stack&& stack);

		~parse_result();
	private:
		Stack stack;
	};
}
