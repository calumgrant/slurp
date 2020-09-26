/*
	Optional header file for pretty-printing types.
	This is useful for debugging the parser.

	The usage is fairly straigforward:

	std::cout << print<X> << std::endl;

	Where X is the type to print. This works for all the types used in Slurp.
*/

#pragma once
#include <iostream>
#include <typeinfo>

namespace slurp
{
	// Prints a type to a stream.
	// The canonical usage is stream << print<T> because
	// the std library overloads operator << to allow things like os << std::endl;
	template<typename T>
	std::ostream& print(std::ostream& os);

	// Print helpers
	namespace helpers
	{
		template<typename T>
		struct pp
		{
			static void output(std::ostream& os) {
				const char* name = typeid(T).name();

				// Clean up the name a bit - this is plaform independent and is not completely portable.
				while (const char* space = strchr(name, ' '))
					name = space + 1;
				while (const char* space = strchr(name, ':'))
					name = space + 1;
				os << name;
			}
		};

		template<>
		struct pp<typeset<>>
		{
			static void output(std::ostream& os) {
				os << "{}";
			}
		};

		template<typename... Ts>
		struct pp_ts;

		template<>
		struct pp_ts<>
		{
			static void output(std::ostream& os) { }
		};

		template<typename T, typename... Ts>
		struct pp_ts<typeset<T, Ts...>>
		{
			static void output(std::ostream& os)
			{
				os << ", " << print<T >> ;
				pp_ts<Ts...>::output(os);
			}
		};


		template<typename T, typename...Ts>
		struct pp<typeset<T,Ts...>>
		{
			static void output(std::ostream& os) {
				os << "{ " << print<T>;
				pp_ts<Ts...>::output(os);
				os << " }";
			}
		};



		template<int Kind, typename T>
		struct pp<Token<Kind, T>>
		{
			static void output(std::ostream& os)
			{
				os << print<T> << ":" << Kind;
			}
		};

		template<int C>
		struct pp<Ch<C>>
		{
			static void output(std::ostream& os)
			{
				os << "'" << (char)C << "'";
			}
		};

		template<int C1, int C2>
		struct pp<Range<C1, C2>>
		{
			static void output(std::ostream& os)
			{
				os << "[" << (char)C1 << "-" << (char)C2 << "]";
			}
		};


		template<typename ... Ts>
		struct pp_list;

		template<>
		struct pp_list<>
		{
			static void output(std::ostream&) { }
		};

		template<typename T, typename...Ts>
		struct pp_list<T, Ts...>
		{
			static void output(std::ostream& os)
			{
				os << " " << print<T>;
				pp_list<Ts...>::output(os);
			}
		};


		template<int Kind, typename...Ts>
		struct pp<Rule<Kind, Ts...>>
		{
			static void output(std::ostream& os)
			{
				os << Kind << " ->";
				pp_list<Ts...>::output(os);
			}
		};

		template<>
		struct pp<Rules<>>
		{
			static void output(std::ostream& os)
			{
			}
		};

		template<typename T, typename...Ts>
		struct pp<Rules<T, Ts...>>
		{
			static void output(std::ostream& os)
			{
				os << "    " << print<T> << std::endl << print<Rules<Ts...>>;
			}
		};

	}

	template<typename T>
	std::ostream & print(std::ostream & os) {
		helpers::pp<T>::output(os);
		return os;
	}
}
