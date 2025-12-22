using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IClimateDataApplicationService
    {
        Task<HomeAssistantDTO> GetHeatingDegreeDays();
        Task<HomeAssistantDTO> GetAverageHeatingDegreeDays(int numberOfYears);
        Task<HomeAssistantDTO> GetMeanTemperature(string stationId);
    }
}