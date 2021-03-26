#pragma once

#include <stack>

namespace slurp
{
	template<typename It>
	class parser
	{
	public:
		typedef parse_result(*parserfn)(It a, It b);
		parser(parserfn fn) : fn(fn) {}

		// A parser is a function that tokenizes a stream and returns an abstract syntax tree.
		parserfn fn;
	};





	namespace helpers
	{
		template<typename Symbol, typename Target>
		struct front_recursive
		{
			static const bool value = front_recursive<typename Symbol::rules, Target>::value;
		};

		template<typename Target>
		struct front_recursive<Target, Target>
		{
			static const bool value = true;
		};

		template<typename Target>
		struct front_recursive<Rules<>, Target>
		{
			static const bool value = false;
		};

		template<typename Target, typename T, typename...Ts>
		struct front_recursive<Rules<T, Ts...>, Target>
		{
			static const bool value = front_recursive<T, Target>::value || front_recursive<Rules<Ts...>, Target>::value;
		};

		template<typename Target, int Kind, typename T>
		struct front_recursive<Token<Kind, T>, Target>
		{
			static const bool value = false;
		};


		template<int Kind, typename Target>
		struct front_recursive<Rule<Kind>, Target>
		{
			static const bool value = false;
		};

		template<int Kind, typename T, typename Target, typename...Ts>
		struct front_recursive<Rule<Kind, T, Ts...>, Target>
		{
			static const bool value = front_recursive<T, Target>::value || is_empty<T>::value && front_recursive<Rule<Kind, Ts...>, Target>::value;
		};
	}

	// Determines whether a symbol is front recursive.
	template<typename Symbol>
	struct front_recursive
	{
		static const bool value = helpers::front_recursive<typename Symbol::rule, Symbol>::value;
	};

	namespace helpers
	{
		template<typename Tokenizer, typename It>
		struct recursive_continuation
		{
			virtual bool call(Tokenizer tok, token_position<It>& pos, Stack& stack) const = 0;
		};

		template<typename Tokenizer, typename It>
		class recursive_stack
		{
			Tokenizer tokenizer;
			Stack stack;
			typedef void(*parse_fn)(recursive_stack<Tokenizer, It>&);

			std::stack<parse_fn> fnstack;

			bool m_success;

			struct rewindpoint
			{
				unsigned stack_size;
				std::size_t fnstacksize;
				parse_fn fn;
			};
			std::stack<rewindpoint> choicepoints;
			token_position<It> pos;
		public:

			bool istoken(short kind)
			{
				return pos.kind == kind;
			}

			void reduce(short kind, int children)
			{
				stack.Reduce(kind, children);
			}

			recursive_stack(parse_fn init, Tokenizer tok, token_position<It> first_token)
			{
				tokenizer = tok;
				pos = first_token;
				push_next(init);
				m_success = false;
				tok.MoveNext(pos);
			}

			void push_next(parse_fn fn)
			{
				fnstack.push(fn);
			}

			void shift_token()
			{
				stack.Shift(pos.kind, pos.data, pos.begin(),pos.end());
			}

			void push_rewind(parse_fn next)
			{
				choicepoints.push(rewindpoint{ stack.Top(), fnstack.size(), next });
			}

			void rewind()
			{
				auto top = choicepoints.top();
				choicepoints.pop();
				// fnstack.resize(top.fnstacksize);
				while (fnstack.size() > top.fnstacksize)
					fnstack.pop();

				stack.Unwind(top.stack_size);
				push_next(top.fn);
			}

			void success()
			{
				m_success = true;
			}


			parse_result parse()
			{
				while (fnstack.size() > 0)
				{
					parse_fn fn = fnstack.top();
					fnstack.pop();
					(*fn)(*this);
					if (m_success) return std::move(stack);
				}
				return parse_result();
			}

			parse_result result()
			{
				return stack;
			}
		};

		template<typename Symbol>
		struct recursive_descent
		{
			static_assert(!slurp::front_recursive<Symbol>::value, "Symbol in recursive descent parser is front-recursive");

			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				return recursive_descent<typename Symbol::rule>::parse(tok, pos, stack, next);
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				stack.push_next(recursive_descent<typename Symbol::rule>::parse2);
			}

		};

		template<int Kind, typename T>
		struct recursive_descent<Token<Kind, T>>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				if (pos.kind == Kind)
				{
					// Push the token onto the stack
					stack.Shift(Kind, pos.data, pos.begin(), pos.end());
					tok.MoveNext(pos);

					return next.call(tok, pos, stack);
				}
				return false;
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				if (stack.istoken(Kind))
					stack.shift_token();
				else
					stack.rewind();
			}

		};

		template<int Kind>
		struct recursive_descent<Rule<Kind>>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				stack.Shift(Kind, t.data, 0);  // A node with no children
				return next.call(tok, pos, stack);
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				stack.shift_empty_rule(Kind);
			}
		};

		template<>
		struct recursive_descent<Rules<>>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer, token_position<It>& t, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				return false;
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
			}
		};

		template<typename H, typename... Ts>
		struct recursive_descent<Rules<H, Ts...>>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				auto save1 = pos;
				auto save2 = stack.Top();
				if (recursive_descent<H>::parse(tok, pos, stack, next))
					return true;

				// Rewind the stack and the tokenizer
				pos = save1;
				stack.Unwind(save2);
				// return true;
				return recursive_descent<Rules<Ts...>>::parse(tok, pos, stack, next);
			};

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				stack.push_rewind(recursive_descent<Rules<Ts...>>::parse2);
				stack.push_next(recursive_descent<H>::parse2);
			}
		};

		template<int Node, int Children, typename... Ts>
		struct recursive_descent_rule
		{
		};

		template<int Node, int Children>
		struct recursive_descent_rule<Node, Children>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				// Successful reduction - there are Children items on the stack
				stack.Reduce(Node, Children);
				return next.call(tok, pos, stack);
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				stack.reduce(Node, Children);
			}
		};

		template<int Node, int Children, typename H, typename...Ts>
		struct recursive_descent_rule<Node, Children, H, Ts...>
		{

			template<typename Tokenizer, typename It>
			class recursive_call : public recursive_continuation<Tokenizer, It>
			{
			public:
				recursive_call(const recursive_continuation<Tokenizer, It>& next) : m_next(next) { }

				const recursive_continuation<Tokenizer, It>& m_next;

				bool call(Tokenizer tok, token_position<It>& pos, Stack& stack) const
				{
					return recursive_descent_rule<Node, Children + 1, Ts...>::parse(tok, pos, stack, m_next);
				};
			};

			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				return recursive_descent<H>::parse(tok, pos, stack, recursive_call<Tokenizer, It>(next));
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				stack.push_next(recursive_descent<H>::parse2);
				stack.push_next(recursive_descent_rule<Node, Children + 1, Ts...>::parse2);
			}

		};


		template<int Node, typename...Ts>
		struct recursive_descent<Rule<Node, Ts...>>
		{
			template<typename Tokenizer, typename It>
			static bool parse(Tokenizer tok, token_position<It>& pos, Stack& stack, const recursive_continuation<Tokenizer, It>& next)
			{
				return recursive_descent_rule<Node, 0, Ts...>::parse(tok, pos, stack, next);
			}

			template<typename Tokenizer, typename It>
			static void parse2(recursive_stack<Tokenizer, It>& stack)
			{
				recursive_descent_rule<Node, 0, Ts...>::parse2(stack);
			}
		};

		template<typename Tokenizer, typename It>
		class recursive_descent_eof : public recursive_continuation<Tokenizer, It>
		{
		public:
			bool call(Tokenizer tok, token_position<It>& pos, Stack& stack) const override
			{
				return pos.kind == -1;
			}
		};
	}

	// Stack-based version do not use
	template<typename Grammar, typename Tokenizer, typename It> parse_result recursive_descent(Tokenizer tok, It a, It b)
	{
		token_position<It> pos(a, b);
		Stack result;
		tok.MoveNext(pos);

		if (helpers::recursive_descent<Grammar>::parse(tok, pos, result, helpers::recursive_descent_eof<Tokenizer, It>()))
			return result;

		return parse_result();
	}

	template<typename Grammar, typename Tokenizer, typename It> parse_result recursive_descent2(Tokenizer tok, It a, It b)
	{
		helpers::recursive_stack<Tokenizer, It> stack(helpers::recursive_descent<Grammar>::parse2, tok, token_position<It>(a,b));

		return stack.parse();
	}


	template<typename Grammar, typename It>
	parser<It> recursive_descent_parser()
	{
		return recursive_descent_parser_fn<Grammar, It>;
	}
}
