using DocSenseV1.Services.RateLimiter;
using Xunit;

namespace DocSenseV1Test.Services.RateLimiter
{
    public class RateLimiterServiceTests
    {
        private readonly IRateLimiterService _rateLimiterService;

        public RateLimiterServiceTests()
        {
            _rateLimiterService = new RateLimiterService();
        }

        [Fact]
        public async Task IsAllowedAsync_FirstRequest_ShouldAllow()
        {
            // Arrange
            var user = "test-key-123";
            var plan = "basic";

            // Act
            var result = await _rateLimiterService.IsAllowedAsync(user, plan);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAllowedAsync_BasicPlanMaxTenRequests_ShouldBlockEleventh()
        {
            // Arrange
            var user = "test-key-free";
            var plan = "basic";

            // Act - делаем 10 запросов (должны пройти)
            for (int i = 0; i < 10; i++)
            {
                var result = await _rateLimiterService.IsAllowedAsync(user, plan);
                Assert.True(result, $"Request {i + 1} should be allowed");
            }

            // 11-й запрос должен быть заблокирован
            var blockedResult = await _rateLimiterService.IsAllowedAsync(user, plan);

            // Assert
            Assert.False(blockedResult);
        }

        [Fact]
        public async Task IsAllowedAsync_ProPlanMaxHundredRequests_ShouldBlockOneHundredFirst()
        {
            // Arrange
            var user = "test-key-pro";
            var plan = "Pro";

            // Act - делаем 100 запросов (должны пройти)
            for (int i = 0; i < 100; i++)
            {
                var result = await _rateLimiterService.IsAllowedAsync(user, plan);
                Assert.True(result, $"Request {i + 1} should be allowed");
            }

            // 101-й запрос должен быть заблокирован
            var blockedResult = await _rateLimiterService.IsAllowedAsync(user, plan);

            // Assert
            Assert.False(blockedResult);
        }

        [Fact]
        public async Task IsAllowedAsync_UltraPlanMaxThousandRequests_ShouldBlockThousandFirst()
        {
            // Arrange
            var user = "test-key-ultra";
            var plan = "Ultra";

            // Act - делаем 1000 запросов (должны пройти)
            for (int i = 0; i < 1000; i++)
            {
                var result = await _rateLimiterService.IsAllowedAsync(user, plan);
                Assert.True(result, $"Request {i + 1} should be allowed");
            }

            // 1001-й запрос должен быть заблокирован
            var blockedResult = await _rateLimiterService.IsAllowedAsync(user, plan);

            // Assert
            Assert.False(blockedResult);
        }

        [Fact]
        public async Task IsAllowedAsync_UnknownPlan_ShouldUseBasicLimit()
        {
            // Arrange
            var user = "test-key-unknown";
            var plan = "Unknown";

            // Act - делаем 10 запросов (Free лимит - должны пройти)
            for (int i = 0; i < 10; i++)
            {
                var result = await _rateLimiterService.IsAllowedAsync(user, plan);
                Assert.True(result);
            }

            // 11-й должен быть заблокирован
            var blockedResult = await _rateLimiterService.IsAllowedAsync(user, plan);

            // Assert
            Assert.False(blockedResult);
        }
    }
}