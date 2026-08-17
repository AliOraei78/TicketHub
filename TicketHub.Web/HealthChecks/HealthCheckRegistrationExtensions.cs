using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TicketHub.Web.HealthChecks;

public static class HealthCheckRegistrationExtensions
{
    public static IServiceCollection AddTicketHubHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "Database (PostgreSQL)",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" })
            .AddCheck<RedisHealthCheck>(
                name: "Cache (Redis / In-Memory)",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis", "ready" })
            .AddCheck<HangfireHealthCheck>(
                name: "Background Jobs (Hangfire)",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "jobs", "hangfire", "ready" });

        return services;
    }

    public static IEndpointRouteBuilder MapTicketHubHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // 1. Detailed JSON Endpoint: /health
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        // 2. Readiness Probe: /health/ready (Verifies all dependencies for traffic routing)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        // 3. Liveness Probe: /health/live (Minimal check for process health / container restart)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        return endpoints;
    }
}
