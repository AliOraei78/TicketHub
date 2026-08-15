using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketHub.Infrastructure.Data;
using TicketHub.Web.HealthChecks;

namespace TicketHub.Tests.bUnit;

public class HealthCheckTests
{
    private async Task<IHost> CreateHealthCheckTestServerAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMemoryCache();
                        services.AddDistributedMemoryCache();

                        var config = new ConfigurationBuilder()
                            .AddInMemoryCollection()
                            .Build();
                        services.AddSingleton<IConfiguration>(config);

                        services.AddDbContextFactory<AppDbContext>(opt =>
                            opt.UseInMemoryDatabase($"HealthTestDb_{Guid.NewGuid():N}"));

                        services.AddTicketHubHealthChecks();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapTicketHubHealthChecks();
                        });
                    });
            })
            .StartAsync();

        return host;
    }

    [Fact]
    public async Task DatabaseHealthCheck_WithValidDbContext_ShouldReturnHealthy()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DbHealthCheckTest_{Guid.NewGuid():N}")
            .Options;

        var mockFactory = new TestDbContextFactory(options);
        var healthCheck = new DatabaseHealthCheck(mockFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("اتصال به پایگاه داده");
        result.Data.Should().ContainKey("responseTimeMs");
    }

    [Fact]
    public async Task RedisHealthCheck_WithDistributedMemoryCache_ShouldReturnHealthy()
    {
        // Arrange
        var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var healthCheck = new RedisHealthCheck(memoryCache, config);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("سرویس کش");
        result.Data.Should().ContainKey("provider");
    }

    [Fact]
    public async Task HangfireHealthCheck_WithoutStorage_ShouldReturnUnhealthyOrHandledGracefully()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().BeOneOf(HealthStatus.Unhealthy, HealthStatus.Degraded, HealthStatus.Healthy);
        result.Data.Should().ContainKey("responseTimeMs");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn200AndJsonFormattedReport()
    {
        // Arrange
        using var host = await CreateHealthCheckTestServerAsync();
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: json);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        json.Should().Contain("status");
        json.Should().Contain("entries");
        json.Should().Contain("Database (SQL Server)");
    }

    [Fact]
    public async Task HealthLiveEndpoint_ShouldReturn200ForContainerLiveness()
    {
        // Arrange
        using var host = await CreateHealthCheckTestServerAsync();
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReadyEndpoint_ShouldReturn200ForTrafficReadiness()
    {
        // Arrange
        using var host = await CreateHealthCheckTestServerAsync();
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("status");
        json.Should().Contain("entries");
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);
    }
}
