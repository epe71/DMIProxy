using DMIProxy.BusinessEntity.MetObs;

namespace DMIProxy.DomainService;

public interface IClimateDataService
{
    public enum ParameterId
    {
        acc_heating_degree_days_17,
        mean_temp
    }

    public Task<DmiMetObsData> GetParameterId(ParameterId parameterId);
}