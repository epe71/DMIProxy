using DMIProxy.DomainService;

namespace DMIProxyTests
{
    [TestClass]
    public class ForecastDataCalculatorTests
    {
        [TestMethod]
        public void DivideTest()
        {
            // Arrange
            var values = new List<double> { 1, 2, 3, 4, 5 };

            // Act
            var adjusted = new AdjustList(values)
                .Divide(2)
                .Run();

            // Assert
            Assert.AreEqual(0.5, adjusted[0]);
            Assert.AreEqual(1, adjusted[1]);
            Assert.HasCount(values.Count, adjusted);
        }

        [TestMethod]
        public void MultiplyTest()
        {
            // Arrange
            var values = new List<double> { 1, 2, 3, 4, 5 };

            // Act
            var adjusted = new AdjustList(values)
                .Multiply(3)
                .Run();

            // Assert
            Assert.AreEqual(3, adjusted[0]);
            Assert.AreEqual(6, adjusted[1]);
            Assert.HasCount(values.Count, adjusted);
        }

        [TestMethod]
        public void SubtractTest()
        {
            // Arrange
            var values = new List<double> { 1, 2, 3, 4, 5 };

            // Act
            var adjusted = new AdjustList(values)
                .Subtract(3)
                .Run();

            // Assert
            Assert.AreEqual(-2, adjusted[0]);
            Assert.AreEqual(-1, adjusted[1]);
            Assert.HasCount(values.Count, adjusted);
        }

        [TestMethod]
        public void RoundTest()
        {
            // Arrange
            var values = new List<double> { 1, 2, 3, 4, 5 };

            // Act
            var adjusted = new AdjustList(values)
                .Divide(3)
                .Round(1)
                .Run();

            // Assert
            Assert.AreEqual(0.3, adjusted[0]);
            Assert.AreEqual(0.7, adjusted[1]);
            Assert.HasCount(values.Count, adjusted);
        }

        [TestMethod]
        public void DifferenceTest()
        {
            // Arrange
            var values = new List<double> { 1, 2, 3, 4, 5 };

            // Act
            var adjusted = new AdjustList(values)
                .Difference()
                .Run();

            // Assert
            Assert.AreEqual(0, adjusted[0]);
            Assert.AreEqual(1, adjusted[1]);
            Assert.HasCount(values.Count, adjusted);
        }
    }
}
