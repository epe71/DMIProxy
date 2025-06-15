using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public interface IEdrService
    {
        Task<List<HomeAssistantDTO>> GetEdrForecast(List<string> forecastParameters);
    }
}