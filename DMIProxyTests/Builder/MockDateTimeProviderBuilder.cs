using DMIProxy.BusinessEntity;
using Moq;

namespace DMIProxyTests.Builder;

public class MockDateTimeProviderBuilder
{
    private DateTime _date;
    public MockDateTimeProviderBuilder()
    {
        _date = DateTime.Now;
    }

    public MockDateTimeProviderBuilder WithDateTime(DateTime dateTime)
    {
        _date = dateTime;
        return this;
    }

    public IDateTimeProvider Build()
    {
        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.Now).Returns(_date);

        return mockDateTime.Object;
    }
}
