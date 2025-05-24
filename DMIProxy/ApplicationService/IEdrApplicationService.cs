using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IEdrApplicationService
    {
        Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter);
    }
}