#pragma once

namespace slurp
{

	template<typename It>
	class parser
	{
	public:
		typedef Stack(*parserfn)(It a, It b);
		parser(parserfn fn) : fn(fn) {}

		// A parser is a function that tokenizes a stream and returns an abstract syntax tree.
		parserfn fn;
	};

	template<typename Symbol>
	struct recursive_descent : public recursive_descent<typename Symbol::rule>
	{
	};

	template<int Kind, typename T>
	struct recursive_descent<Token<Kind, T>>
	{
		template<typename Tokenizer, typename It, typename Fn>
		static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, Fn next)
		{
			if (pos.kind == Kind)
			{
				// Push the token onto the stack
				stack.Shift(Kind, pos.data, pos.begin(), pos.end());
				tok.MoveNext(pos);

				return next(tok, pos, stack);
			}
			return false;
		}
	};

	template<int Kind>
	struct recursive_descent<Rule<Kind>>
	{
		template<typename Tokenizer, typename It, typename Fn>
		static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, Fn next)
		{
			stack.Shift(Kind, t.data, 0);  // A node with no children
			return next(tok, pos, stack);
		}
	};

	template<>
	struct recursive_descent<Rules<>>
	{
		template<typename Tokenizer, typename It, typename Next>
		static bool parse(Tokenizer, token_position<It>& t, Stack& stack, Next next)
		{
			return false;
		}
	};

	template<typename H, typename... Ts>
	struct recursive_descent<Rules<H, Ts...>>
	{
		template<typename Tokenizer, typename It, typename Fn>
		static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, Fn next)
		{
			auto save1 = pos;
			auto save2 = stack.Top();
			if (recursive_descent<H>::parse(tok, pos, stack, next))
				return true;

			// Rewind the stack and the tokenizer
			pos = save1;
			stack.Unwind(save2);
			return recursive_descent<Rules<Ts...>>::parse(tok, pos, stack, next);
		};
	};

	template<int Node, int Children, typename... Ts>
	struct recursive_descent_rule
	{
	};

	template<int Node, int Children>
	struct recursive_descent_rule<Node, Children>
	{
		template<typename Tokenizer, typename It, typename Next>
		static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, Next next)
		{
			// Successful reduction - there are Children items on the stack
			stack.Reduce(Node, Children);
			return next(tok, pos, stack);
		}
	};

	template<int Node, int Children, typename H, typename...Ts>
	struct recursive_descent_rule<Node, Children, H, Ts...>
	{
		template<typename Tokenizer, typename It, typename Next>
		static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, Next next)
		{
			return recursive_descent<H>::parse(tok, pos, stack, [&next](Tokenizer & tok2, token_position<It>& pos2, Stack& stack2)
				{
					return true;
					// return recursive_descent_rule<Node, Children + 1, Ts...>::parse(tok2, pos2, stack2, next);
				});
		}
	};


	template<int Node, typename...Ts>
	struct recursive_descent<Rule<Node, Ts...>> : public recursive_descent_rule<Node, 0, Ts...>
	{
	};

	template<typename Grammar, typename Tokenizer, typename It> Stack recursive_descent2(Tokenizer tok, It a, It b)
	{
		token_position<It> pos(a, b);
		Stack result;
		tok.MoveNext(pos);

		recursive_descent<Grammar>::parse(tok, pos, result, [](Tokenizer tok2, token_position<It> pos2, Stack& stack)
			{
				return pos2.kind == -1;
			}
		);


		// if(recursive_descent<Grammer>::parse(t.

		return result;
	}

	template<typename Grammar, typename It>
	parser<It> recursive_descent_parser()
	{
		return recursive_descent_parser_fn<Grammar, It>;
	}
}
