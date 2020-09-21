/*
 * This sample demonstrates a calclator written using the Slurp parser generator.
 * See https://github.com/calumgrant/slurp
 * 
 * The calculator accepts the numerical operators such as +, -, * and /
 * It accepts numbers in integer, decimal and IEEE formats
 * Expressions are grouped according to operator precedence,
 * i.e. 1+2*3 is interpreted as 1+(2*3).
 * Brackets () group expressions
 * The power operator ^ associates to the left
 * Mathematical constants are pi, e
 * Mathematical operations are sin, cos, tan, exp, ln and floor.
 * The factorial function is postfix !
 */

using System;
using System.Linq;
using Slurp;

namespace Calculator
{
    class Program
    {
        /*
         * A function that creates the parser.
         * 
         * A parser, IParser<>, is a function that converts a string into a another type,
         * in this case a double.
         * 
         * The parser is defined using a grammar. A grammar is a set of rules that
         * define how an input string can be structured. Ideally a grammar is
         * unambiguous, meaning that there is only one way to structure an input string
         * according to the rules of the grammar.
         * 
         * In Slurp, the grammar is defined using a set of symbols. Terminal symbols
         * are represented by variables of type Terminal, and non-terminal symbols have type Symbol<>.
         * In this example, all of the symbols evaluate to double, so all the non-terminal symbols have
         * the type Symbol<double>.
         * 
         * Terminal symbols can be defined using a regex-style syntax.
         * 
         * The non-terminal symbols are defined using the "Match" function to define the
         * production rules of the grammar.
         * 
         * Finally, the MakeParser() function compiles the grammar into a parser.
         */
        internal static IParser<double> CreateParser()
        {
            // The "terminal symbols" of the grammar

            // Define a terminal symbol called "integer", matching the patterm [1-9][0-9]*
            // The + operator concatenates terminals
            // The Repeat method repeats terminals, and takes the place of
            // * + ? {} found in regular expressions
            // Terminal.Digit is shorthand for Terminal.Range('0'..'9')
            // Terminal.Range matches a range of charaters
            Terminal integer = '0' | Terminal.Range('1'..'9') + Terminal.Digit.Repeat(0..);

            // The | operator selects a choice
            Terminal @decimal = integer | (integer + '.' + Terminal.Digit.Repeat(0..)) | ('.' + Terminal.Digit.Repeat(0..));

            // The OneOf method selects one of
            Terminal @float = @decimal + (Terminal.OneOf('e', 'E') + Terminal.OneOf('+', '-').Repeat(0..1) + integer).Repeat(0..1);
            @float.Name = "num";

            // A terminal consisting of a single character, +
            Terminal plus = '+';
            Terminal minus = '-';

            // The "non-terminal symbols" of the grammar.
            // In the calculator grammar, all non-terminal symbols generate a
            // "double" when the are parsed.
            var Expression = new Symbol<double>();
            var PrimaryExpression = new Symbol<double>();
            var AdditiveExpression = new Symbol<double>();
            var MultiplicativeExpression = new Symbol<double>();
            var UnaryExpression = new Symbol<double>();
            var PostfixExpression = new Symbol<double>();

            // Define the rules of the grammar

            // Add a rule to the PrimaryExpression symbol.
            // The "Match" function takes a sequence of terminals,
            // and the last argument is a function that takes the result of the three sub-rules,
            // and returns a double, the result of the expression.
            PrimaryExpression.Match((Terminal)'(', Expression, (Terminal)')', (x,y,z) => y);

            // The last argument is a function that transforms a token into a double (the result of the expression).
            // This rule converts a Token into a "double".
            PrimaryExpression.Match(@float, x => double.Parse(x.Text));

            // This rule converts a token "nan" into a double with value "Nan".
            // since the token is always the text "nan", we can discard the text of the token.
            PrimaryExpression.Match((Terminal)"nan", _ => double.NaN);
            PrimaryExpression.Match((Terminal)"inf", _ => double.PositiveInfinity);
            PrimaryExpression.Match((Terminal)"pi", _ => Math.PI);
            PrimaryExpression.Match((Terminal)"e", _ => Math.E);

            PostfixExpression.Match(PrimaryExpression);
            PostfixExpression.Match(PostfixExpression, (Terminal)'!', (x, _) => Factorial(x));

            // Note that the ^ operator associates to the right,
            // which means that the expression 2^3^4 is parsed as 2^(3^4).
            // This is unlike the + and * operators which associate to the left.
            PostfixExpression.Match(PrimaryExpression, (Terminal)'^', PostfixExpression, (x, y, z) => Math.Pow(x, z));

            UnaryExpression.Match(PostfixExpression);
            UnaryExpression.Match(plus, UnaryExpression, (_, y) => y);
            UnaryExpression.Match(minus, UnaryExpression, (_, y) => -y);
            UnaryExpression.Match((Terminal)"ln", UnaryExpression, (_, y) => Math.Log(y));
            UnaryExpression.Match((Terminal)"sin", UnaryExpression, (_, y) => Math.Sin(y));
            UnaryExpression.Match((Terminal)"cos", UnaryExpression, (_, y) => Math.Cos(y));
            UnaryExpression.Match((Terminal)"tan", UnaryExpression, (_, y) => Math.Tan(y));
            UnaryExpression.Match((Terminal)"exp", UnaryExpression, (_, y) => Math.Exp(y));
            UnaryExpression.Match((Terminal)"floor", UnaryExpression, (_, y) => Math.Floor(y));

            MultiplicativeExpression.Match(UnaryExpression);
            MultiplicativeExpression.Match(MultiplicativeExpression, (Terminal)'*', UnaryExpression, (x, _, z) => x * z);
            MultiplicativeExpression.Match(MultiplicativeExpression, (Terminal)'/', UnaryExpression, (x, _, z) => x / z);

            AdditiveExpression.Match(AdditiveExpression, plus, MultiplicativeExpression, (x, _, z) => x + z);
            AdditiveExpression.Match(AdditiveExpression, minus, MultiplicativeExpression, (x, _, z) => x - z);
            AdditiveExpression.Match(MultiplicativeExpression);

            Expression.Match(AdditiveExpression);

            // MakeParser compiles a parser for the grammar.
            // A tokeniser is automatically generated based on the terminal symbols that occur in the grammar.
            return Expression.MakeParser(ParserGenerator.CLR);
        }

        /* 
         * The entry point into the program.
         * This constructs a parser, prompts the user for an expression, uses the parser to evaluate
         */
        static void Main(string[] args)
        {
            Console.WriteLine("Enter an expression:");

            var parser = CreateParser();

            string line = "";
            do
            {
                try
                {
                    Console.Write("> ");
                    line = Console.ReadLine();
                    Console.WriteLine(parser.Parse(line));
                }
                catch (SyntaxError e)
                {
                    Console.WriteLine($"Syntax error at {e.ErrorToken.Text}");
                    if(e.ExpectedSymbols.Any())
                    {
                        Console.Write("Expected: ");
                        foreach (var s in e.ExpectedSymbols)
                        {
                            Console.Write(s); Console.Write(" ");
                        }
                        Console.WriteLine();
                    }
                }
            }
            while (!string.IsNullOrEmpty(line));
        }

        // An incorrect factorial function
        // Factorial isn't defined for non-integers.
        static double Factorial(double n)
        {
            if (n < 0) return double.NaN;
            if (n > 200) return double.PositiveInfinity;  // DoS guard :-)
            double result = 1;
            for (; n > 1; result = result * n, n = n - 1) ;
            return result;
        }
    }
}
