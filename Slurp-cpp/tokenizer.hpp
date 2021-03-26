
namespace slurp
{
	template<typename It>
	class token_position
	{
	public:

		token_position() : kind(-1)
		{
		}

		token_position(It stream_start, It stream_end) : tok_end(stream_start), stream_end(stream_end)
		{ 
		}

		typedef typename std::iterator_traits<It>::difference_type difference_type;

		// The number of characters in the token.
		difference_type size() const {
			return end() - begin();
		}

		operator bool() const
		{
			return tok_start != strean_end;
		}

		// An iterator over the characters in the token.
		typedef It iterator;

		// The beginning of the characters in the token
		iterator begin() const {
			return tok_start;
		}

		// The end of the characters in the token.
		iterator end() const {
			return tok_end;
		}

		// Information about the row/column/offset of the token
		// Not all tokenizers populate this data.
		TokenData data;

		It tok_start, tok_end, stream_end;
		
		// The kind of the token
		// -1 for end of stream / error
		short kind;

		bool operator==(const token_position<It>& other) const
		{
			return tok_start == other.tok_start;
		}

		bool operator!=(const token_position<It>& other) const
		{
			return tok_start != other.tok_start;
		}
	};




	template<typename Tokens, int Symbol>
	struct advance;

	template<int Symbol>
	struct advance<ts_empty, Symbol>
	{
		typedef ts_empty type;
	};

	template<int Symbol, typename H, typename...Ts>
	struct advance<typeset<H, Ts...>, Symbol>
	{
		typedef typename advance<H, Symbol>::type t1;
		typedef typename advance<typeset<Ts...>, Symbol>::type t2;
		typedef typename ts_concat<t1, t2>::type type;
	};

	template<int Symbol, int Kind, typename T>
	struct advance<Token<Kind, T>, Symbol>
	{
		typedef Token<Kind, typename advance<T, Kind>::type> type;
	};

	struct accept;
	struct reject;

	template<int C>
	struct advance<Ch<C>, C>
	{
		typedef accept type;
	};

	template<int C1, int C2>
	struct advance<Ch<C2>, C1>
	{
		typedef reject type;
	};


	template<typename It, typename Tokens>
	void yylex(token_position<It>& it)
	{
		switch (*it.tok_end)
		{
		case 0: return yylex<It, typename advance<Tokens, 0>::type>(it);
		}
	}


	// A tokenizer that turns characters into tokens.
	// This is used mainly for tests, or if you want the
	// parser to also do the tokenizing for some reason.
	struct null_tokenizer
	{
		template<typename It>
		void MoveNext(token_position<It>& pos)
		{
			if (pos.tok_end == pos.stream_end)
			{
				pos.kind = -1;
			}
			else
			{
				pos.tok_start = pos.tok_end;
				pos.kind = *pos.tok_start;
				pos.tok_end = pos.tok_start + 1;
			}
		}
	};
}
