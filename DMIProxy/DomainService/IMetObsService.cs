using DMIProxy.BusinessEntity.MetObs;

namespace DMIProxy.DomainService
{
    public interface IMetObsService
    {
        Task<DmiMetObsData> GetRain(string stationId);
    }
}
