using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketHub.Web.Security;

namespace TicketHub.Tests.bUnit;

public class RateLimitingTests
{
    private async Task<IHost> CreateTestServerAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddTicketHubRateLimiting();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRateLimiter();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/test-auth", () => Results.Ok("auth ok"))
                                .RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

                            endpoints.MapGet("/test-antispam", () => Results.Ok("antispam ok"))
                                .RequireRateLimiting(RateLimitingExtensions.AntiSpamPolicy);

                            endpoints.MapGet("/test-unlimited", () => Results.Ok("unlimited ok"));
                        });
                    });
            })
            .StartAsync();

        return host;
    }

    [Fact]
    public async Task AuthPolicy_ShouldAllowUpToPermitLimit_AndRejectSubsequentRequestsWith429()
    {
        // Arrange
        using var host = await CreateTestServerAsync();
        var client = host.GetTestClient();

        // Act - First 5 requests should succeed (PermitLimit = 5)
        for (int i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/test-auth");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 6th request should be rejected (Rate limited)
        var rejectedResponse = await client.GetAsync("/test-auth");

        // Assert
        rejectedResponse.StatusCode.Should().Be((HttpStatusCode)429);
        var content = await rejectedResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Too Many Requests");
        content.Should().Contain("تعداد درخواست‌های ارسالی بیش از حد مجاز است");
    }

    [Fact]
    public async Task AntiSpamPolicy_ShouldAllowAllowedRequests_AndEnforceSlidingLimit()
    {
        // Arrange
        using var host = await CreateTestServerAsync();
        var client = host.GetTestClient();

        // Act - First 10 requests succeed (PermitLimit = 10)
        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/test-antispam");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Subsequent burst requests that exceed permit and queue limit
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 5; i++)
        {
            lastResponse = await client.GetAsync("/test-antispam");
            if (lastResponse.StatusCode == (HttpStatusCode)429)
                break;
        }

        // Assert
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task UnlimitedEndpoint_ShouldNotBeAffectedByEndpointRateLimit()
    {
        // Arrange
        using var host = await CreateTestServerAsync();
        var client = host.GetTestClient();

        // Act
        for (int i = 0; i < 15; i++)
        {
            var response = await client.GetAsync("/test-unlimited");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
