using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService
{
    public interface IMetObsService
    {
        Task<DmiResult> GetRain(string stationId);
    }
}
