using DMIProxy.Contract;
using System.Net;
using System.Collections.ObjectModel;

namespace DMIProxy.DomainService;

public sealed record EdrForecastResult(HttpStatusCode StatusCode, Collection<HomeAssistantDTO> Forecasts);
