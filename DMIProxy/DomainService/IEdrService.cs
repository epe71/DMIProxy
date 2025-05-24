using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public interface IEdrService
    {
        Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter);
    }
}