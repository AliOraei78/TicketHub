using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Web.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DatabaseHealthCheck(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var isInMemory = dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
            var canConnect = isInMemory || await dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("عدم امکان برقراری ارتباط با پایگاه داده SQL Server.",
                    data: new Dictionary<string, object>
                    {
                        ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                        ["provider"] = dbContext.Database.ProviderName ?? "Unknown"
                    });
            }

            return HealthCheckResult.Healthy("اتصال به پایگاه داده SQL Server پایدار و فعال است.",
                data: new Dictionary<string, object>
                {
                    ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["provider"] = dbContext.Database.ProviderName ?? "SqlServer"
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy($"خطا در بررسی سلامت پایگاه داده: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["responseTimeMs"] = stopwatch.ElapsedMilliseconds
                });
        }
    }
}
