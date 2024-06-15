using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IEdrApplicationService
    {
        Task<ForcastDTO> GetForcast();
        Task<HomeAssistantDTO> GetCloudForcast();
    }
}