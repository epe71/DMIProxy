using DMIProxy.DomainService;
using DMIProxyTests.Builder;

namespace DMIProxyTests
{
    [TestClass]
    public class TimeSpanCalculatorTests
    {
        [DataTestMethod]
        [DataRow(0, 5)]
        [DataRow(6, 10)]
        [DataRow(12, 4)]
        [DataRow(18, 11)]
        public void TimespanToFixTime(int hour, int hourSpan)
        {
            // Arrange
            var mockDateTime = new MockDateTimeProviderBuilder()
                .WithDateTime(new DateTime(2025, 3, 30, hour, 1, 20, DateTimeKind.Local))
                .Build();

            var timeSpanCalculator = new TimeSpanCalculator(mockDateTime);

            // Act 
            var updateTime = new List<TimeOnly>()
            {
                new TimeOnly(6, 0),
                new TimeOnly(17, 0)
            };
            var result = timeSpanCalculator.FixTime(updateTime);
            Console.WriteLine(mockDateTime.Now);
            Console.WriteLine(result);
            Console.WriteLine(mockDateTime.Now + result);

            // Assert
            Assert.AreEqual(hourSpan, result.Hours);
            Assert.AreEqual(58, result.Minutes);
            Assert.AreEqual(40, result.Seconds);
        }

        [DataTestMethod]
        [DataRow(0, 3, 4, 2)]
        [DataRow(0, 33, 3, 32)]
        public void AtTheTopOfThehour(int hour, int minutes, int spanHour, int spanMinute)
        {
            // Arrange
            var mockDateTime = new MockDateTimeProviderBuilder()
                .WithDateTime(new DateTime(2025, 3, 30, hour, minutes, 20, DateTimeKind.Local))
                .Build();
            var timeSpanCalculator = new TimeSpanCalculator(mockDateTime);

            // Act 
            var result = timeSpanCalculator.AtTheTopOfTheHour(4);

            // Assert
            Assert.AreEqual(spanHour, result.Hours);
            Assert.AreEqual(spanMinute, result.Minutes);
            Assert.AreEqual(0, result.Seconds);
        }
    }
}
