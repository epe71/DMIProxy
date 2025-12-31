using DMIProxy.Contract;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        bool GetAllEdrKeys(out Dictionary<string, DateTime>? keys);
        List<string> GetEdrKeysToUpdate(string key);
        void EdrKeyUpdated(string key);

        MemoryCacheStatistics? CacheStatistics();
    }
}
