using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IClimateDataApplicationService
    {
        Task<HomeAssistantDTO> GetHeatingDegreeDays();
        Task<HomeAssistantDTO> GetMeanTemperature(string stationId);
    }
}