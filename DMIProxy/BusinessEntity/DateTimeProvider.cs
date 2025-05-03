namespace DMIProxy.BusinessEntity;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now.ToLocalTime();

    public DateTime UtcNow => DateTime.UtcNow;
}
