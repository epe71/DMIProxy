using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService;

public class TimeSpanCalculator(IDateTimeProvider dateTimeProvider) : ITimeSpanCalculator
{
    /// <summary>
    /// Calculate the time to the top of the hour (+ 5 min and some random seconds)
    /// </summary>
    /// <param name="hours">the number of hour to skip</param>
    /// <returns></returns>
    public TimeSpan AtTheTopOfTheHour(int hours)
    {
        var now = dateTimeProvider.Now;
        var minutesToTopOfTheHour = 5 + 60 - now.Minute;
        int minutesToExpire = (hours - 1) * 60 + minutesToTopOfTheHour;
        var randomDelay = new TimeSpan(0, 0, 0, now.Second);
        return TimeSpan.FromMinutes(minutesToExpire).Add(randomDelay);
    }

    /// <summary>
    /// Calculate the timespan to the next time of the day when the time is equal to the given hour and minute ether AM or PM
    /// </summary>
    /// <param name="updateTime">A list of hour and minutes when it is time to update</param>
    /// <returns></returns>
    public TimeSpan FixTime(List<TimeOnly> updateTime)
    {
        var now = dateTimeProvider.Now;
        var expirationTime = NextExpirationTime(now, updateTime);

        return expirationTime - now;
    }

    private DateTime NextExpirationTime(DateTime now, List<TimeOnly> updateTime)
    {
        var today = DateOnly.FromDateTime(now);
        foreach (var t in updateTime)
        {
            var expirationTime = new DateTime(today.Year, today.Month, today.Day, t.Hour, t.Minute, 0);
            if (expirationTime > now)
            {
                return expirationTime;
            }
        }

        // If no time is found, return the first time in the list
        today = today.AddDays(1);
        var time = updateTime.First();
        return new DateTime(today.Year, today.Month, today.Day, time.Hour, time.Minute, 0);
    }
}
