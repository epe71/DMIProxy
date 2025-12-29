using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService;

public class MetObsApplicationService(
    IMetObsService metObsService,
    ITimeSpanCalculator timeSpanCalculator,
    IFusionCache cache) : IMetObsApplicationService
{
    public async Task<RainDTO> GetRain(string stationId)
    {
        var expirationTime = timeSpanCalculator.AtTheTopOfTheHour(3);
        var rainDto = await cache.GetOrSetAsync<RainDTO>(
            $"Rain-{stationId}",
            async (_, _) => await GetRain_NoCache(stationId),
            options => options.SetDuration(expirationTime)
        );

        return rainDto ?? throw new InvalidOperationException("Rain data could not be retrieved.");
    }

    private async Task<RainDTO> GetRain_NoCache(string stationId)
    {
        DmiMetObsData result = await metObsService.GetRain(stationId);
        if (result == null)
        {
            // Handle the case where result is null
            throw new InvalidOperationException("Failed to retrieve rain data.");
        }
        var rainDto = new RainDTO()
        {
            Rain1h = result.Rain1h(),
            RainToday = result.RainToday(),
            RainThisMonth = result.RainThisMonth(),
            TimeStamp = result.timeStamp,
            NumberReturned = result.numberReturned
        };

        return rainDto;
    }
}