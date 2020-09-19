using Slurp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace tests
{
    class ParserTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]

        public void TokenizerTests()
        {
            var token = (Terminal)"foo";
            var bar = (Terminal)"bar";

            var tokenizer = new Tokenizer(token, bar);
            Assert.AreEqual(-2, tokenizer.Match("123"));
            Assert.AreEqual(0, tokenizer.Match("foo"));
            Assert.AreEqual(1, tokenizer.Match("bar"));
            Assert.AreEqual(-2, tokenizer.Match("foot"));
            Assert.AreEqual(-1, tokenizer.Match("f"));

            var tokens = tokenizer.Tokenize("barfoo").ToArray();
            Assert.AreEqual("bar", tokens[0].Text);
            Assert.AreEqual(1, tokens[0].TokenId);
            Assert.AreEqual("foo", tokens[1].Text);
            Assert.AreEqual(0, tokens[1].TokenId);

            tokens = tokenizer.Tokenize(" bar  foo ").ToArray();
            Assert.AreEqual(tokens.Length, 6);
            Assert.AreEqual(" ", tokens[0].Text);
            Assert.AreEqual("bar", tokens[1].Text);
            Assert.AreEqual(" ", tokens[2].Text);
            Assert.AreEqual(" ", tokens[3].Text);
            Assert.AreEqual("foo", tokens[4].Text);
            Assert.AreEqual(" ", tokens[5].Text);
        }

        [Test]
        public void EmptyTokenizer()
        {
            var tokenizer1 = new Tokenizer("");

            var tokenizer2 = new Tokenizer();
        }

        [Test]
        public void TestRange()
        {
            Tokenizer t;
            t = new Tokenizer(Terminal.Range('1', '1'));
            Assert.AreEqual(-2, t.Match("0"));
            Assert.AreEqual(0, t.Match("1"));
            Assert.AreEqual(-2, t.Match("2"));

            t = new Tokenizer(Terminal.Range('1', '2'));
            Assert.AreEqual(-2, t.Match("0"));
            Assert.AreEqual(0, t.Match("1"));
            Assert.AreEqual(0, t.Match("2"));
            Assert.AreEqual(-2, t.Match("3"));



            t = new Tokenizer(Terminal.Digit);
            Assert.AreEqual(-2, t.Match("a"));
            Assert.AreEqual(0, t.Match("0"));
            Assert.AreEqual(0, t.Match("8"));
            Assert.AreEqual(0, t.Match("9"));
        }

        [Test]
        public void TestAnyChar()
        {
            Tokenizer t;
            t = new Tokenizer(Terminal.AnyChar);
            Assert.AreEqual(0, t.Match("x"));
        }

        [Test]
        public void OverlappingRules()
        {

            Tokenizer t;
            Assert.Throws<UnmatchableTokenException>(() => new Tokenizer("a",'a'));
            Assert.Throws<UnmatchableTokenException>(() => new Tokenizer('a', "a"));
            t = new Tokenizer("a");
            Assert.AreEqual(0, t.Match("a"));

            t = new Tokenizer('a');
            Assert.AreEqual(0, t.Match("a"));

            t = new Tokenizer("a", "ab");
            var tokens = t.Tokenize("abcaba").ToArray();
        }

        [Test]
        public void UmatchableRule()
        {
            var integer = Terminal.Digit;
            Assert.Throws<UnmatchableTokenException>(()=>new Tokenizer(integer, "123", "1", Terminal.AnyChar));
            Assert.Throws<UnmatchableTokenException>(() => new Tokenizer(Terminal.Digit.Repeat(1..), "123", "1", Terminal.AnyChar));

            var tokenizer1 = new Tokenizer(integer, "123", "_", Terminal.AnyChar);
            var tokens1 = tokenizer1.Tokenize("567,123,1,1").ToArray();

            Assert.Throws<UnmatchableTokenException>(()=>new Tokenizer(Terminal.AnyChar, "x"));
            var tokenizer2 = new Tokenizer("x", Terminal.AnyChar);
            var tokens2 = tokenizer1.Tokenize("xxxyyyzzz").ToArray();
        }

        [Test]
        public void RepeatTests()
        {
            Tokenizer t;
            t = new Tokenizer(Terminal.Char('a').Repeat(0..3));

            Assert.AreEqual(0, t.Match(""));
            Assert.AreEqual(0, t.Match("a"));
            Assert.AreEqual(0, t.Match("aa"));
            Assert.AreEqual(0, t.Match("aaa"));
            Assert.AreEqual(-2, t.Match("aaaa"));

            t = new Tokenizer(Terminal.Digit.Repeat(1..), Terminal.Alpha.Repeat(1..));
            Assert.AreEqual(0, t.Match("123"));
            Assert.AreEqual(1, t.Match("hello"));
        }

        [Test]
        public void NotTests()
        {
            Tokenizer t;

            Terminal quote = '\"';
            Terminal @string = quote + quote.Not.Repeat(0..) + quote;
            Terminal alpha = Terminal.Range('a'..'z') | Terminal.Range('A'..'Z');
            Terminal digit = Terminal.Range('0'..'9');
            Terminal alnum = alpha | digit;
            Terminal @identifier = alpha + alnum.Repeat(0..);
            Terminal @int = digit.Repeat(1..);
            t = new Tokenizer(@string, identifier, @int);

            var tokens = t.Tokenize("\"hello\" \"\" 123 barrel X").ToArray();
            Assert.AreEqual("\"hello\"", tokens[0].Text);
            Assert.AreEqual(" ", tokens[1].Text);
            Assert.AreEqual("\"\"", tokens[2].Text);
            Assert.AreEqual("123", tokens[4].Text);
            Assert.AreEqual("barrel", tokens[6].Text);
            Assert.AreEqual("X", tokens[8].Text);
        }

        [Test]
        public void ParserTests2()
        {
            Terminal a = 'a';
            Terminal b = 'b';
            Symbol<string> ab = new Symbol<string>();
            ab.Match(a, b, (Token a, Token b) => a.Text + b.Text);

            var tokenizer = ab.MakeTokenizer();
        }

        [Test]
        public void TestLR0()
        {
            Terminal a = 'a';

            Symbol<string> Id = new Symbol<string>();
            Id.Match(a, a, a, (x, y, z) => x.Text + y.Text + z.Text);


            var lr0 = Id.MakeParser(ParserGenerator.LR0);

            Terminal i = 'i';
            Terminal plus = '+';
            Terminal open = '(';
            Terminal close = ')';
            Symbol<string> E = new Symbol<string>("E");
            Symbol<string> T = new Symbol<string>("T");

            E.Match(T);
            E.Match(E, plus, T, (e, _, t) => e + t);
            T.Match(i, x => x.Text);
            T.Match(open, E, close, (_0, e, _1) => e);

            var parser = E.MakeParser(ParserGenerator.LR0);

            // Use the grammar for parsing

            var s = parser.Parse("i");
            Assert.AreEqual("i", s);
            s = parser.Parse("i+i");
            Assert.AreEqual("ii", s);
            s = parser.Parse("i+(i)");
            Assert.AreEqual("ii", s);

            Assert.Throws<SyntaxError>(() => parser.Parse("i)"));
        }

        [Test]
        public void TestLR0ReduceReduceConflict()
        {
            Terminal a = 'a';
            var P = new Symbol<int>();

            P.Match(a, x => 1);
            P.Match(a, x => 2);

            Assert.Throws<ReduceReduceConflict>(() => P.MakeParser(ParserGenerator.LR0));
        }

        [Test]
        public void TestSLRParser()
        {
            // See
            // https://en.wikipedia.org/wiki/LR_parser#Constructing_LR.280.29_parsing_tables
            // "Conflicts in the contructed tables"

            Terminal one = '1';
            Symbol<int> E = new Symbol<int>("E");

            E.Match(one, _ => 1);
            E.Match(one, E, (x, y) => 1 + y);

            Assert.Throws<ShiftReduceConflict>(() => E.MakeParser(ParserGenerator.LR0));

            var p0 = E.MakeParser(ParserGenerator.SLR);


            Symbol<int> E2 = new Symbol<int>("E2");
            var A = new Symbol<int>("A");
            var B = new Symbol<int>("B");

            Terminal two = '2';
            E2.Match(A, one, (a, b) => a + 1);
            E2.Match(B, two, (a, b) => a + 2);
            A.Match(one, a => 1);
            B.Match(one, a => 1);

            Assert.Throws<ReduceReduceConflict>(() => E2.MakeParser(ParserGenerator.LR0));            
        }

        [Test]
        public void SimpleGrammar()
        {
            // See: http://david.tribble.com/text/lrk_parsing.html

            Terminal num = Terminal.Digit;
            Terminal plus = '+';
            Terminal open = '(';
            Terminal close = ')';

            var Expr = new Symbol<int>("Expr");
            var Factor = new Symbol<int>("Factor");

            Expr.Match(Factor);
            Expr.Match(open, Expr, close, (_t1, n, _t) => n);
            Factor.Match(num, k => int.Parse(k.Text));
            Factor.Match(plus, Factor, (_t, n) => n);
            Factor.Match(Factor, plus, num, (f, _, num) => f + int.Parse(num.Text));


            var tok = Expr.MakeTokenizer();

            var parser = Expr.MakeParser(ParserGenerator.SLR);

            // var result = parser.Parse("1+1");

            // Manual construction of the action and goto tables
        }


        [Test]
        public void ManualTable()
        {
            // Implement https://en.wikipedia.org/wiki/LR_parser

            Terminal times = '*';
            Terminal plus = '+';
            Terminal _0 = '0';
            Terminal _1 = '1';

            var E = new Symbol<int>("E");
            var B = new Symbol<int>("B");

            E.Match(E, times, B, (x,t,b) => x * b);
            E.Match(E, plus, B, (x,t,b) => x + b);
            E.Match(B);
            B.Match(_0, token => 0);
            B.Match(_1, token => 1);

            var tok = E.MakeTokenizer();

            var parser = E.MakeParser(ParserGenerator.SLR);

            // var result = parser.Parse("1+1");

            // Manual construction of the action and goto tables
        }

        [Test]
        public void ReduceReduceConflict()
        {
            Terminal _1 = '1';
            
            var G = new Symbol<int>("G");
            G.Match(_1, (token) => 1);
            G.Match(_1, (token) => 2);

            Assert.Throws<ReduceReduceConflict>(() => G.MakeParser(ParserGenerator.SLR));
        }

        [Test]
        public void DegenerateRule()
        {
            var E = new Symbol<int>("E");
            E.Match(E);  // Invalid

            var p = E.MakeParser(ParserGenerator.SLR);

            // TODO: Mutually invalid rules.
        }

        [Test]
        public void TestLALR()
        {
            // https://en.wikipedia.org/wiki/LALR_parser

            // Non-terminal symbols in the grammar:
            var S = new Symbol<int>();
            var E = new Symbol<int>();
            var F = new Symbol<int>();

            // Terminal synbols in the grammar:
            Terminal a = 'a';
            Terminal b = 'b';
            Terminal c = 'c';
            Terminal d = 'd';
            Terminal e = 'e';

            // Grammar rules:
            S.Match(a, E, c, (x,y,z) => 1);
            S.Match(a, F, d, (x, y, z) => 2);
            S.Match(b, F, c, (x, y, z) => 3);
            E.Match(e, x => 4);
            F.Match(e, x => 5);

            // S.MakeParser(ParserGenerator.LR0);
            Assert.Throws<ReduceReduceConflict>(()=>S.MakeParser(ParserGenerator.SLR));
        }

        [Test]

        public void TestLexerRegression()
        {
            Terminal t1 = "123";
            Terminal t2 = "12345";
            var tok = new Tokenizer(t1, t2);
            var tokens = tok.Tokenize("12345").ToArray();
            Assert.AreEqual(1, tokens.Length);
        }

        [Test]
        public void TestCalculator()
        {
            Terminal integer = Terminal.Range('1'..'9') + Terminal.Digit.Repeat(0..);

            // The | operator selects a choice
            Terminal @decimal = integer | (integer + '.' + Terminal.Digit.Repeat(0..)) | ('.' + Terminal.Digit.Repeat(0..));

            // The OneOf method selects one of
            Terminal @float = @decimal + (Terminal.OneOf('e', 'E') + Terminal.OneOf('+', '-').Repeat(0..1) + integer).Repeat(0..1);

            var tok = new Tokenizer(@float);
            var tokens = tok.Tokenize("123").ToArray();
            Assert.AreEqual(1, tokens.Length);
            var tokens1 = tok.Tokenize("123e12").ToArray();
            Assert.AreEqual(1, tokens1.Length);
            var tokens2 = tok.Tokenize("123e-12").ToArray();
            Assert.AreEqual(1, tokens2.Length);
            var tokens3 = tok.Tokenize("123e+12").ToArray();
            Assert.AreEqual(1, tokens3.Length);

            // Test the grammar as well.
        }

        [Test]
        public void testCLR1()
        {
            // See https://www.youtube.com/watch?v=UOVQQq_dOn8
            Terminal c = 'c';
            Terminal d = 'd';

            Symbol<int> S = new Symbol<int>("S");
            Symbol<int> C = new Symbol<int>("C");

            S.Match(C, C, (a, b) => a + b);
            C.Match(c, C, (x, y) => 2 + y);
            C.Match(d, x => 1);

            var p = S.MakeParser(ParserGenerator.CLR);
        }
    }

    class ExampleGrammar
    {
        static ExampleGrammar()
        {
            // Example 1: parse a comma-separated list of integers

            var comma = Terminal.Char(',');
            var whitespace = Terminal.OneOf(' ', '\t').Repeat(1..);

            var integer = new Symbol<int>();
            var sequence = new Symbol<List<int>>();

            integer.Match(Terminal.Digit.Repeat(1..), s => int.Parse(s.Text));

            sequence.Match(integer, i => new List<int>(i));
            sequence.Match(sequence, comma, integer, (s, _, i) => { s.Add(i); return s; });

            var sequence_parser = sequence.MakeParser(ParserGenerator.SLR);

            // Example 2: An arithmetic expression parser

            var expression = new Symbol<int>();
            var additive_expression = new Symbol<int>();
            var primary_expession = new Symbol<int>();
            var multiplicative_expression = new Symbol<int>();
            var primary_expression = new Symbol<int>();
            var unary_expression = new Symbol<int>();

            expression.Match(primary_expression);

            primary_expression.Match(Terminal.Char('('), expression, Terminal.Char(')'), (_1, e, _2) => e);
            primary_expression.Match(integer);
            primary_expression.Match(additive_expression);

            additive_expression.Match(additive_expression, (Terminal)'+', multiplicative_expression, (e1, _, e2) => e1 + e2);
            additive_expression.Match(additive_expression, (Terminal)'-', multiplicative_expression, (e1, _, e2) => e1 - e2);
            additive_expression.Match(multiplicative_expression);

            multiplicative_expression.Match(multiplicative_expression, (Terminal)'*', unary_expression, (e1, _, e2) => e1 * e2);
            multiplicative_expression.Match(multiplicative_expression, (Terminal)'/', unary_expression, (e1, _, e2) => e1 / e2);
            multiplicative_expression.Match(unary_expression);

            unary_expression.Match(primary_expression);
            unary_expression.Match(Terminal.Char('-'), unary_expression, (_, e) => -e);

            var expression_parser = expression.MakeParser(ParserGenerator.SLR);
            var six = expression_parser.Parse("1+2+3");

            // Example 3: JSON parser
            Terminal regularWs = Terminal.OneOf(' ', '\t').Repeat(1..);
            Terminal newline = Terminal.Char('\r').Repeat(0..1) + '\n';
            Terminal cppcomment = "//" + Terminal.Char('\n').Not.Repeat(0..);
            Terminal ccomment = "/*" + (Terminal.Char('*').Not | Terminal.Char('*')).Repeat(0..) + "*/";
            Terminal comment = ccomment | cppcomment;
            Terminal anyWs = regularWs | newline | comment;

            Terminal stringLiteral = Terminal.Char('\"') + (!Terminal.OneOf('\"', '\r', '\n')).Repeat(0..) + Terminal.Char('\"');

        }
    }
}
