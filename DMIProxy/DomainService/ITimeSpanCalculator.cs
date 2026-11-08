using System.Collections.ObjectModel;

namespace DMIProxy.DomainService;

public interface ITimeSpanCalculator
{
    TimeSpan AtTheTopOfTheHour(int hours);
    TimeSpan FixTime(Collection<TimeOnly> updateTime);
}
