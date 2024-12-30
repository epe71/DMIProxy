using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IEdrApplicationService
    {
        Task<HomeAssistantDTO> GetEdrForcast(string forcastParameter);
    }
}