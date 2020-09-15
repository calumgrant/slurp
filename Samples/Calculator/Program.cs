using System;

using Slurp;


namespace Calculator
{
    class Program
    {
        /// <summary>
        /// Defines the parser.
        /// </summary>
        /// <returns>A parser that converts a string to a double.</returns>
        static IParser<double> CreateParser()
        {
            // The "terminal symbols" of the grammar

            // Define a terminal symbol called "integer", matching the patterm [1-9][0-9]*
            // The + operator concatenates terminals
            // The Repeat method repeats terminals, and takes the place of
            // * + ? {} found in regular expressions
            // Terminal.Digit is shorthand for Terminal.Range('0'..'9')
            // Terminal.Range matches a range of charaters
            Terminal integer = Terminal.Range('1'..'9') + Terminal.Digit.Repeat(0..);

            // The | operator selects a choice
            Terminal @decimal = integer | (integer + '.' + Terminal.Digit.Repeat(0..)) | ('.' + Terminal.Digit.Repeat(0..));

            // The OneOf method selects one of
            Terminal @float = @decimal + (Terminal.OneOf('e', 'E') + Terminal.OneOf('+', '-').Repeat(0..1) + integer).Repeat(0..1);

            // A terminal consisting of a single character, +
            Terminal plus = '+';
            Terminal minus = '-';
            Terminal times = '*';
            Terminal slash = '/';
            Terminal nan = "nan";
            Terminal inf = "inf";

            // A terminal consisting of a string, **
            Terminal starstar = "**";
            Terminal open = '(';
            Terminal close = ')';

            //  The "non-terminal symbols" of the grammar
            var Expression = new Symbol<double>();
            var PrimaryExpression = new Symbol<double>();
            var AdditiveExpression = new Symbol<double>();
            var MultiplicativeExpression = new Symbol<double>();
            var PowerExpression = new Symbol<double>();
            var UnaryExpression = new Symbol<double>();

            // Define the rules of the grammar
            
            // PrimaryExpression -> ( Expression )
            // The last argument is a function that takes the result of the three sub-rules,
            // and returns a double, the result of the expression.
            PrimaryExpression.Match(open, Expression, close, (x,y,z) => y);

            // PrimaryExpression -> @decimal
            // The last argument is a function that transforms a token into a double (the result of the expression).
            PrimaryExpression.Match(@float, x => double.Parse(x.Text));

            PrimaryExpression.Match(nan, x => double.NaN);
            PrimaryExpression.Match(inf, x => double.PositiveInfinity);
            PrimaryExpression.Match((Terminal)"pi", x => Math.PI);
            PrimaryExpression.Match((Terminal)"e", x => Math.E);

            UnaryExpression.Match(PrimaryExpression);
            UnaryExpression.Match(plus, UnaryExpression, (x, y) => y);
            UnaryExpression.Match(minus, UnaryExpression, (x, y) => -y);
            UnaryExpression.Match((Terminal)"ln", UnaryExpression, (x, y) => Math.Log(y));
            UnaryExpression.Match((Terminal)"sin", UnaryExpression, (x, y) => Math.Sin(y));
            UnaryExpression.Match((Terminal)"cos", UnaryExpression, (x, y) => Math.Cos(y));
            UnaryExpression.Match((Terminal)"tan", UnaryExpression, (x, y) => Math.Tan(y));
            UnaryExpression.Match((Terminal)"exp", UnaryExpression, (x, y) => Math.Exp(y));
            UnaryExpression.Match((Terminal)"floor", UnaryExpression, (x, y) => Math.Floor(y));

            // AdditiveExpression -> AdditiveExpression + MultiplicativeExpression
            AdditiveExpression.Match(AdditiveExpression, plus, UnaryExpression, (x, y, z) => x + z);

            // AdditiveExpression -> AdditiveExpression - MultiplicativeExpression
            AdditiveExpression.Match(AdditiveExpression, minus, UnaryExpression, (x, y, z) => x - z);
            AdditiveExpression.Match(UnaryExpression);

            MultiplicativeExpression.Match(AdditiveExpression);
            MultiplicativeExpression.Match(MultiplicativeExpression, (Terminal)'*', AdditiveExpression, (x, y, z) => x * z);
            MultiplicativeExpression.Match(MultiplicativeExpression, (Terminal)'/', AdditiveExpression, (x, y, z) => x / z);

            PowerExpression.Match(MultiplicativeExpression);
            PowerExpression.Match(MultiplicativeExpression, starstar, PowerExpression, (x, y, z) => Math.Pow(x, z));

            // Expression -> PowerExpression
            // No need to supply a function here as the result is untransformed
            Expression.Match(AdditiveExpression);

            // Compiles a parser for the grammar.
            // A tokeniser is automatically generated as well, based on the terminal symbols
            // that occur in the grammar.
            // It is also possible to tweak the tokenizer, but that's not needed in this simple case.
            return Expression.MakeParser();
        }

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
                    foreach (var token in parser.Tokenizer.Tokenize(line))
                        Console.WriteLine(token);
                    Console.WriteLine(parser.Parse(line));
                }
                catch (SyntaxError e)
                {
                    Console.WriteLine($"Syntax error at {e.ErrorToken.Text}");
                }
            }
            while (!string.IsNullOrEmpty(line));
        }
    }
}
