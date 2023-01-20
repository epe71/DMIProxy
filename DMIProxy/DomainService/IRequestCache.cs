using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        void ClearCache();
        bool GetRainDTO(string stationId, out RainDTO? rainDto);
        void SaveRainDTO(string stationId, RainDTO rainDTO);
    }
}
