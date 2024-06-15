using Microsoft.AspNetCore.Rewrite;

namespace DMIProxy.Contract
{
    public class HomeAssistantDTO
    {
        public List<PointDTO> data { get; init; }
        public string description { get; init; }
    }
}
