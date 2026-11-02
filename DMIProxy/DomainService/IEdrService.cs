namespace DMIProxy.DomainService
{
    public interface IEdrService
    {
        Task<EdrForecastResult> GetEdrForecast(List<string> forecastParameters);
    }
}
