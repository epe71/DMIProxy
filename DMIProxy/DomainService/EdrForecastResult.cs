using DMIProxy.Contract;
using System.Net;

namespace DMIProxy.DomainService;

public sealed record EdrForecastResult(HttpStatusCode StatusCode, List<HomeAssistantDTO> Forecasts);
