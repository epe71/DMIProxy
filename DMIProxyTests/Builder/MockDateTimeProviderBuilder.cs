using DMIProxy.BusinessEntity;
using Moq;

namespace DMIProxyTests.Builder;

public class MockDateTimeProviderBuilder
{
    private DateTime _date;
    private Mock<IDateTimeProvider> mockDateTime;

    public MockDateTimeProviderBuilder()
    {
        _date = DateTime.Now;
        mockDateTime = new Mock<IDateTimeProvider>();
    }

    public MockDateTimeProviderBuilder WithDateTime(DateTime dateTime)
    {
        _date = dateTime;
        mockDateTime.Setup(d => d.Now).Returns(_date);
        return this;
    }

    public MockDateTimeProviderBuilder WithUtcDateTime(DateTime dateTime)
    {
        mockDateTime.Setup(d => d.UtcNow).Returns(dateTime);
        return this;
    }

    public MockDateTimeProviderBuilder WithUtcTimeSequnce(List<DateTime> dateTimes)
    {
        var sequence = mockDateTime.SetupSequence(d => d.UtcNow);
        foreach (var dateTime in dateTimes)
        {
            sequence.Returns(dateTime);
        }
        return this;
    }

    public IDateTimeProvider Build()
    {
        return mockDateTime.Object;
    }
}
