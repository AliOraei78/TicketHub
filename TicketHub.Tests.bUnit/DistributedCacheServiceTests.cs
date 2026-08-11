using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TicketHub.Infrastructure.Services;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class DistributedCacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DistributedCacheService>> _mockLogger;
    private readonly DistributedCacheService _service;

    public DistributedCacheServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DistributedCacheService>>();
        _service = new DistributedCacheService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsDeserializedObject_WhenKeyExistsInCache()
    {
        // Arrange
        var testData = new TestCacheModel { Id = 1, Name = "TestItem" };
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testData));
        _mockCache.Setup(c => c.GetAsync("test_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonBytes);

        // Act
        var result = await _service.GetAsync<TestCacheModel>("test_key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TestItem", result.Name);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyDoesNotExistInCache()
    {
        // Arrange
        _mockCache.Setup(c => c.GetAsync("missing_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetAsync<TestCacheModel>("missing_key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_HandlesCacheException_WithGracefulFallback()
    {
        // Arrange (Simulating Redis Server Offline)
        _mockCache.Setup(c => c.GetAsync("offline_key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection failed"));

        // Act
        var result = await _service.GetAsync<TestCacheModel>("offline_key");

        // Assert: Should NOT throw exception, must return null (Graceful Fallback)
        Assert.Null(result);
    }

    private class TestCacheModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
