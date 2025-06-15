namespace DMIProxy.Contract
{
    public class HomeAssistantDTO
    {
        public string name { get; init; }
        public List<PointDTO> data { get; init; }
        public string description { get; init; }
    }
}
