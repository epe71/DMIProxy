using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public interface IEdrService
    {
        Task<ForcastDTO> GetForcast();
        Task<HomeAssistantDTO> GetCloudForcast();
    }
}