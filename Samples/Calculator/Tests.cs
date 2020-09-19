using NUnit.Framework;
using Slurp;


namespace Calculator
{
    public class Tests22
    {
        IParser<double> parser;

        [SetUp]
        public void Setup()
        {
            parser = Program.CreateParser();
        }

        [Test]
        public void CalculatorTests()
        {
            Assert.AreEqual(1, parser.Parse("1"));
            Assert.AreEqual(10, parser.Parse("1e1"));
        }

        [Test]
        public void CalculatorTests2()
        {
            Assert.AreEqual(1, parser.Parse("1"));
            Assert.AreEqual(10, parser.Parse("1e1"));
        }
    }
}
