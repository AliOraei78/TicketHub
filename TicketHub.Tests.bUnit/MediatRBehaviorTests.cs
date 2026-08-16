using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.Behaviors;
using TicketHub.Application.Interfaces;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class MediatRBehaviorTests
{
    [Fact]
    public async Task CachingBehavior_ReturnsCachedResponse_WithoutCallingNextHandler_WhenCacheHit()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<CachingBehavior<TestCacheRequest, string>>>();

        mockCacheService.Setup(s => s.GetAsync<string>("test_cache_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Cached Result");

        var behavior = new CachingBehavior<TestCacheRequest, string>(mockCacheService.Object, mockLogger.Object);
        var request = new TestCacheRequest { CacheKey = "test_cache_key" };

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("Fresh Result");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Cached Result"); nextCalled.Should().BeFalse("Next handler should not be invoked on Cache Hit!");
    }

    [Fact]
    public async Task CachingBehavior_InvokesNextHandlerAndSetsCache_WhenCacheMiss()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<CachingBehavior<TestCacheRequest, string>>>();

        mockCacheService.Setup(s => s.GetAsync<string>("test_cache_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var behavior = new CachingBehavior<TestCacheRequest, string>(mockCacheService.Object, mockLogger.Object);
        var request = new TestCacheRequest { CacheKey = "test_cache_key" };

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Fresh Result");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Fresh Result"); mockCacheService.Verify(s => s.SetAsync("test_cache_key", "Fresh Result", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public class TestCacheRequest : ICacheableRequest
    {
        public string CacheKey { get; set; } = "test_cache_key";
        public TimeSpan? ExpirationRelativeToNow => TimeSpan.FromMinutes(5);
    }
}
