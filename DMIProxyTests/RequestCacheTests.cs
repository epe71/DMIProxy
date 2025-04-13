using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxyTests
{
    [TestClass]
    public class RequestCacheTests
    {
        [TestMethod]
        public void SaveRainDTO_ShouldSaveRainDTO()
        {
            // Arrange
            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            var dateTimeProvider = new DateTimeProvider();
            ITimeSpanCalculator timeSpanCalculator = new TimeSpanCalculator(dateTimeProvider);

            var requestCache = new RequestCache(memoryCache, timeSpanCalculator);
            var rainDTO = new RainDTO
            {
                Rain1h = 1,
                RainToday = 2
            };

            // Act
            requestCache.SaveRainDTO("1234", rainDTO);
            requestCache.GetRainDTO("1234", out var cacheItem);

            // Assert
            Assert.AreEqual(rainDTO.Rain1h, cacheItem?.Rain1h);
            Assert.AreEqual(rainDTO.RainToday, cacheItem?.RainToday);
        }
    }
}
