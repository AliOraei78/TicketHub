using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TicketHub.Web.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;

    public RedisHealthCheck(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var redisConn = _configuration.GetConnectionString("Redis");
        var provider = string.IsNullOrEmpty(redisConn) ? "In-Memory DistributedCache" : "Redis Cache Server";

        try
        {
            var testKey = $"__healthcheck_ping_{Guid.NewGuid():N}";
            await _cache.SetStringAsync(testKey, "healthy", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var value = await _cache.GetStringAsync(testKey, cancellationToken);
            await _cache.RemoveAsync(testKey, cancellationToken);
            stopwatch.Stop();

            if (value != "healthy")
            {
                return HealthCheckResult.Degraded($"پاسخ کش با مقدار مورد انتظار تطابق ندارد ({provider}).",
                    data: new Dictionary<string, object>
                    {
                        ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                        ["provider"] = provider
                    });
            }

            return HealthCheckResult.Healthy($"اتصال به سرویس کش ({provider}) فعال و با موفقیت آزمایش شد.",
                data: new Dictionary<string, object>
                {
                    ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["provider"] = provider
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy($"عدم برقراری ارتباط با سرویس کش ({provider}): {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["provider"] = provider
                });
        }
    }
}
