using NUnit.Framework;
using Slurp;


namespace Calculator
{
    class Tests
    {
        static IParser<double> parser = Program.CreateParser();

        [Test]
        public void CalculatorTests()
        {
            Assert.AreEqual(1, parser.Parse("1"));

        }
    }
}
