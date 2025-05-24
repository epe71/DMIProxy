using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxyTests
{
    [TestClass]
    public class RequestCacheTests
    {
        private RequestCache CreateRequestCache(out IMemoryCache memoryCache, out ITimeSpanCalculator timeSpanCalculator, out IDateTimeProvider dateTimeProvider)
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions());
            dateTimeProvider = new DateTimeProvider();
            timeSpanCalculator = new TimeSpanCalculator(dateTimeProvider);
            return new RequestCache(memoryCache, timeSpanCalculator, dateTimeProvider);
        }

        [TestMethod]
        public void SaveRainDTO_ShouldSaveRainDTO()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            var rainDTO = new RainDTO { Rain1h = 1, RainToday = 2 };

            // Act
            requestCache.SaveRainDTO("1234", rainDTO);
            requestCache.GetRainDTO("1234", out var cacheItem);

            // Assert
            Assert.AreEqual(rainDTO.Rain1h, cacheItem?.Rain1h);
            Assert.AreEqual(rainDTO.RainToday, cacheItem?.RainToday);
        }

        [TestMethod]
        public void SaveEdrKeys_TwoDistinctKeys_ShouldStoreBoth()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            // Act
            requestCache.SaveEdrKey("key1");
            requestCache.SaveEdrKey("key2");
            requestCache.GetEdrKeys(out var keys);

            // Assert
            Assert.IsNotNull(keys);
            Assert.AreEqual(2, keys.Count);
        }

        [TestMethod]
        public void SaveEdrKeys_SameKeyTwice_ShouldUpdateKey()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            // Act
            requestCache.SaveEdrKey("key1");
            var firstTime = GetEdrKeyTime(requestCache, "key1");
            Task.Delay(10).Wait(); // ensure a small delay for update
            requestCache.SaveEdrKey("key1");
            var secondTime = GetEdrKeyTime(requestCache, "key1");

            // Assert
            Assert.IsNotNull(firstTime);
            Assert.IsNotNull(secondTime);
            Assert.IsTrue(secondTime > firstTime);
        }

        private DateTime GetEdrKeyTime(RequestCache cache, string key)
        {
            cache.GetEdrKeys(out var keys);
            return keys != null && keys.TryGetValue(key, out var time) ? time : DateTime.MinValue;
        }

        [TestMethod]
        public void SaveTextForcast_ShouldSaveAndRetrieve()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            var dto = new ForcastMessageDTO { Time = DateTime.UtcNow, Headline = "headline", Message = "message" };

            // Act
            requestCache.SaveTextForcast("station1", dto);
            requestCache.GetTextForcast("station1", out var cached);

            // Assert
            Assert.IsNotNull(cached);
            Assert.AreEqual(dto.Headline, cached.Headline);
            Assert.AreEqual(dto.Message, cached.Message);
        }

        [TestMethod]
        public void SaveEdrForcastDTO_ShouldSaveAndRetrieve()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            var dto = new HomeAssistantDTO { data = new List<PointDTO>(), description = "desc" };

            // Act
            requestCache.SaveEdrForcastDTO("param1", dto);
            requestCache.GetEdrForcastDTO("param1", out var cached);

            // Assert
            Assert.IsNotNull(cached);
            Assert.AreEqual(dto.description, cached.description);
        }

        [TestMethod]
        public void GetRainDTO_ShouldReturnFalseIfNotFound()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            // Act
            var found = requestCache.GetRainDTO("notfound", out var dto);

            // Assert
            Assert.IsFalse(found);
            Assert.IsNull(dto);
        }

        [TestMethod]
        public void SaveEdrKeys_ConcurrentAccess_ShouldMaintainCorrectCount()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            // Act
            Parallel.For(0, 50, i =>
            {
                // Use a few repeating keys
                requestCache.SaveEdrKey("concurrentKey" + (i % 5));
            });
            requestCache.GetEdrKeys(out var keys);

            // Assert: There should be exactly 5 distinct keys
            Assert.IsNotNull(keys);
            Assert.AreEqual(5, keys.Count);
        }
    }
}
