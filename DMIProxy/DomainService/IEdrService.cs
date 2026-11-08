using System.Collections.ObjectModel;

namespace DMIProxy.DomainService;

public interface IEdrService
{
    Task<EdrForecastResult> GetEdrForecast(Collection<string> forecastParameters);
}
