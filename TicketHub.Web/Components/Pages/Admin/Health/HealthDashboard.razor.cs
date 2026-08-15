using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TicketHub.Web.Components.Pages.Admin.Health;

public partial class HealthDashboard : ComponentBase
{
    [Inject]
    public HealthCheckService HealthService { get; set; } = default!;

    private bool _isLoading = true;
    private HealthStatus _overallStatus = HealthStatus.Healthy;
    private double _totalDurationMs = 0;
    private string _lastCheckedTime = "-";
    private long _processMemoryMb = 0;
    private long _gcAllocatedMb = 0;
    private string _appUptime = "-";
    private int _healthyServicesCount = 0;
    private int _totalServicesCount = 0;

    private List<HealthCheckDisplayItem> _entries = new();

    protected override async Task OnInitializedAsync()
    {
        await RefreshHealthAsync();
    }

    private async Task RefreshHealthAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            var report = await HealthService.CheckHealthAsync();
            _overallStatus = report.Status;
            _totalDurationMs = report.TotalDuration.TotalMilliseconds;
            _lastCheckedTime = DateTime.Now.ToString("HH:mm:ss");

            var currentProcess = Process.GetCurrentProcess();
            _processMemoryMb = currentProcess.WorkingSet64 / (1024 * 1024);
            _gcAllocatedMb = GC.GetTotalMemory(false) / (1024 * 1024);

            var uptime = DateTime.Now - currentProcess.StartTime;
            _appUptime = uptime.TotalHours >= 1 
                ? $"{(int)uptime.TotalHours} ساعت و {uptime.Minutes} دقیقه" 
                : $"{uptime.Minutes} دقیقه و {uptime.Seconds} ثانیه";

            _entries = report.Entries.Select(e => new HealthCheckDisplayItem
            {
                Name = e.Key,
                Status = e.Value.Status,
                Description = e.Value.Description,
                DurationMs = e.Value.Duration.TotalMilliseconds,
                Tags = e.Value.Tags.ToList(),
                Data = e.Value.Data?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "-")
            }).ToList();

            _totalServicesCount = _entries.Count;
            _healthyServicesCount = _entries.Count(e => e.Status == HealthStatus.Healthy);
        }
        catch (Exception)
        {
            _overallStatus = HealthStatus.Unhealthy;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private string GetOverallStatusBgGradient() => _overallStatus switch
    {
        HealthStatus.Healthy => "from-emerald-500 to-teal-600",
        HealthStatus.Degraded => "from-amber-500 to-orange-600",
        _ => "from-rose-500 to-red-600"
    };

    private string GetOverallStatusBadgeClass() => _overallStatus switch
    {
        HealthStatus.Healthy => "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20",
        HealthStatus.Degraded => "bg-amber-500/10 text-amber-400 border border-amber-500/20",
        _ => "bg-rose-500/10 text-rose-400 border border-rose-500/20"
    };

    private string GetOverallStatusDotClass() => _overallStatus switch
    {
        HealthStatus.Healthy => "bg-emerald-400",
        HealthStatus.Degraded => "bg-amber-400",
        _ => "bg-rose-400"
    };

    private string GetOverallStatusText() => _overallStatus switch
    {
        HealthStatus.Healthy => "سیستم پایدار و سالم",
        HealthStatus.Degraded => "عملکرد همراه با افت (Degraded)",
        _ => "خطا در سلامت سرویس‌ها"
    };

    private string GetServiceCardBorder(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "border-slate-800/80 hover:border-emerald-500/40",
        HealthStatus.Degraded => "border-amber-800/60 hover:border-amber-500/60",
        _ => "border-rose-800/60 hover:border-rose-500/60"
    };

    private string GetServiceIconBg(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "bg-emerald-500/10 text-emerald-400",
        HealthStatus.Degraded => "bg-amber-500/10 text-amber-400",
        _ => "bg-rose-500/10 text-rose-400"
    };

    private string GetStatusBadge(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20",
        HealthStatus.Degraded => "bg-amber-500/10 text-amber-400 border border-amber-500/20",
        _ => "bg-rose-500/10 text-rose-400 border border-rose-500/20"
    };

    private string GetStatusDot(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "bg-emerald-400",
        HealthStatus.Degraded => "bg-amber-400",
        _ => "bg-rose-400"
    };

    private string GetStatusPersianText(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "سالم",
        HealthStatus.Degraded => "هشدار / افت",
        _ => "قطع / ناسالم"
    };

    private RenderFragment GetServiceIcon(string name) => builder =>
    {
        if (name.Contains("Database", StringComparison.OrdinalIgnoreCase) || name.Contains("SQL", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" class=""w-5 h-5"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4"" />
            </svg>");
        }
        else if (name.Contains("Cache", StringComparison.OrdinalIgnoreCase) || name.Contains("Redis", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" class=""w-5 h-5"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M13 10V3L4 14h7v7l9-11h-7z"" />
            </svg>");
        }
        else
        {
            builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" class=""w-5 h-5"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"" />
            </svg>");
        }
    };
}

public class HealthCheckDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public double DurationMs { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string>? Data { get; set; }
}
