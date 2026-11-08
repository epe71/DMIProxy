using DMIProxy.BusinessEntity;
using Moq;
using System.Collections.ObjectModel;

namespace DMIProxyTests.Builder;

/// <summary>
/// Build DateTime Provider mock objects for testing purposes. This builder allows you to set up specific DateTime
/// values to be returned by the mock IDateTimeProvider, enabling controlled testing of time-dependent logic.
/// </summary>
public class MockDateTimeProviderBuilder
{
    private DateTime _date;
    private readonly Mock<IDateTimeProvider> mockDateTime;

    /// <summary>
    /// Default constructor
    /// </summary>
    public MockDateTimeProviderBuilder()
    {
        _date = DateTime.Now;
        mockDateTime = new Mock<IDateTimeProvider>();
    }

    /// <summary>
    /// Set DateTime.Now
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public MockDateTimeProviderBuilder WithDateTime(DateTime dateTime)
    {
        _date = dateTime;
        mockDateTime.Setup(d => d.Now).Returns(_date);
        return this;
    }

    /// <summary>
    /// Set DateTime.UtcNow
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public MockDateTimeProviderBuilder WithUtcDateTime(DateTime dateTime)
    {
        mockDateTime.Setup(d => d.UtcNow).Returns(dateTime);
        return this;
    }

    /// <summary>
    /// Set a sequence of DateTime.UtcNow
    /// </summary>
    /// <param name="dateTimes"></param>
    /// <returns></returns>
    public MockDateTimeProviderBuilder WithUtcTimeSequnce(Collection<DateTime> dateTimes)
    {
        ArgumentNullException.ThrowIfNull(dateTimes);

        var sequence = mockDateTime.SetupSequence(d => d.UtcNow);
        foreach (var dateTime in dateTimes)
        {
            sequence.Returns(dateTime);
        }
        return this;
    }

    /// <summary>
    /// Build the Mock
    /// </summary>
    /// <returns></returns>
    public IDateTimeProvider Build()
    {
        return mockDateTime.Object;
    }
}
