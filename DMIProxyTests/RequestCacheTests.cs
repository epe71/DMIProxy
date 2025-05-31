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
            requestCache.EdrKeyUpdated("key1");
            requestCache.EdrKeyUpdated("key2");
            requestCache.GetAllEdrKeys(out var keys);

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
            requestCache.EdrKeyUpdated("key1");
            var firstTime = GetEdrKeyTime(requestCache, "key1");
            Task.Delay(10).Wait(); // ensure a small delay for update
            requestCache.EdrKeyUpdated("key1");
            var secondTime = GetEdrKeyTime(requestCache, "key1");

            // Assert
            Assert.IsNotNull(firstTime);
            Assert.IsNotNull(secondTime);
            Assert.IsTrue(secondTime > firstTime);
        }

        private DateTime GetEdrKeyTime(RequestCache cache, string key)
        {
            cache.GetAllEdrKeys(out var keys);
            return keys != null && keys.TryGetValue(key, out var time) ? time : DateTime.MinValue;
        }

        [TestMethod]
        public void SaveTextForecast_ShouldSaveAndRetrieve()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            var dto = new ForecastMessageDTO { Time = DateTime.UtcNow, Headline = "headline", Message = "message" };

            // Act
            requestCache.SaveTextForecast("station1", dto);
            requestCache.GetTextForecast("station1", out var cached);

            // Assert
            Assert.IsNotNull(cached);
            Assert.AreEqual(dto.Headline, cached.Headline);
            Assert.AreEqual(dto.Message, cached.Message);
        }

        [TestMethod]
        public void SaveEdrForecastDTO_ShouldSaveAndRetrieve()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            var dto = new HomeAssistantDTO { data = new List<PointDTO>(), description = "desc" };

            // Act
            requestCache.SaveEdrForecastDTO("param1", dto);
            requestCache.GetEdrForecastDTO("param1", out var cached);

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
                requestCache.EdrKeyUpdated("concurrentKey" + (i % 5));
            });
            requestCache.GetAllEdrKeys(out var keys);

            // Assert: There should be exactly 5 distinct keys
            Assert.IsNotNull(keys);
            Assert.AreEqual(5, keys.Count);
        }

        [TestMethod]
        public void GetEdrKeysToUpdate_FirstCallWithNewKey_ReturnsEmptyString()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
            string newKey = "test-key";

            // Act
            var result = requestCache.GetEdrKeysToUpdate(newKey);

            // Assert
            Assert.AreEqual(string.Empty, result);
            requestCache.GetAllEdrKeys(out var keys);
            Assert.IsNotNull(keys);
            Assert.IsTrue(keys.ContainsKey(newKey), "Key1 not found");
            Assert.IsTrue(keys[newKey] < dateTimeProvider.Now - requestCache.edrKeyTimeOut, "Key1 should not be expired");
        }

        [TestMethod]
        public void GetEdrKeysToUpdate_SecondCallWithKey_ReturnsAllKeys()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            requestCache.GetEdrKeysToUpdate("Key1");
            requestCache.GetEdrKeysToUpdate("Key2");

            // Act
            var result = requestCache.GetEdrKeysToUpdate("Key1");

            // Assert
            Assert.AreEqual("Key1, Key2", result);
            requestCache.GetAllEdrKeys(out var keys);
            Assert.IsNotNull(keys);
            Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
            Assert.IsTrue(keys["Key1"] > dateTimeProvider.UtcNow - requestCache.edrKeyTimeOut, "Key1 should be expired");
        }

        [TestMethod]
        public void GetEdrKeysToUpdate_SecondCallWithKeyAfterUpdate_ReturnsEmptyString()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            requestCache.GetEdrKeysToUpdate("Key1");
            requestCache.EdrKeyUpdated("Key1");

            // Act
            var result = requestCache.GetEdrKeysToUpdate("Key1");

            // Assert
            Assert.AreEqual(string.Empty, result);
            requestCache.GetAllEdrKeys(out var keys);
            Assert.IsNotNull(keys);
            Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
            Assert.IsTrue(keys["Key1"] > dateTimeProvider.Now - requestCache.edrKeyTimeOut, "Key1 should not be expired");
        }

        [TestMethod]
        public void Concurrent_SaveAndGet_RainDTO_ShouldWorkCorrectly()
        {
            // Arrange
            var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

            // Act
            Parallel.For(0, 100, i =>
            {
                // Each thread saves a RainDTO with values based on iteration index.
                var rainDTO = new RainDTO { Rain1h = i % 5, RainToday = i % 3 };
                requestCache.SaveRainDTO("concurrent", rainDTO);
            });
            requestCache.GetRainDTO("concurrent", out var result);

            // Assert
            Assert.IsNotNull(result);
            // Verify that the resulting values fall within the expected ranges.
            Assert.IsTrue(result.Rain1h >= 0 && result.Rain1h <= 4, "Rain1h");
            Assert.IsTrue(result.RainToday >= 0 && result.RainToday <= 2, "RainToday");
        }
    }
}
