namespace DMIProxy.DomainService;

public interface ITimeSpanCalculator
{
    TimeSpan AtTheTopOfTheHour(int hours);
    TimeSpan FixTime(List<TimeOnly> updateTime);
}
