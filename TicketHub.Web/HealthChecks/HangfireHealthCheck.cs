using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TicketHub.Web.HealthChecks;

public class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var jobStorage = JobStorage.Current;
            if (jobStorage == null)
            {
                stopwatch.Stop();
                return Task.FromResult(HealthCheckResult.Degraded("فضای ذخیره‌سازی Hangfire پیکربندی نشده است.",
                    data: new Dictionary<string, object> { ["responseTimeMs"] = stopwatch.ElapsedMilliseconds }));
            }

            var monitoringApi = jobStorage.GetMonitoringApi();
            var servers = monitoringApi.Servers();
            var stats = monitoringApi.GetStatistics();
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["activeServersCount"] = servers.Count,
                ["recurringJobsCount"] = stats.Recurring,
                ["enqueuedJobsCount"] = stats.Enqueued,
                ["failedJobsCount"] = stats.Failed,
                ["servers"] = servers.Select(s => s.Name).ToArray(),
                ["responseTimeMs"] = stopwatch.ElapsedMilliseconds
            };

            if (servers.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "سرور فعال Hangfire یافت نشد (کارهای دوره‌ای ممکن است در صف انتظار بمانند).",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"سرویس پس‌زمینه Hangfire با {servers.Count} سرور فعال و {stats.Recurring} جاب دوره‌ای در حال اجراست.",
                data: data));
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            return Task.FromResult(HealthCheckResult.Degraded(
                "سرویس پس‌زمینه Hangfire در این محیط فعال یا مقداردهی نشده است.",
                exception: ex,
                data: new Dictionary<string, object> { ["responseTimeMs"] = stopwatch.ElapsedMilliseconds }));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(HealthCheckResult.Degraded(
                $"هشدار در بررسی وضعیت Hangfire: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object> { ["responseTimeMs"] = stopwatch.ElapsedMilliseconds }));
        }
    }
}
