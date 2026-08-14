using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TicketHub.Application.Common;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;

namespace TicketHub.Web.Components.Pages.Admin.SystemLogs;

public partial class SystemLogs : ComponentBase, IDisposable
{
    [Inject] public ISystemLogService LogService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    protected List<LogEntry> allFilteredLogs = new();
    protected HashSet<int> expandedRowIndexes = new();
    protected HashSet<LogEntry> selectedLogs = new();
    protected int? recentlyCopiedRowId;

    protected string searchTerm = string.Empty;
    protected string selectedLevel = string.Empty;
    protected string fromDateStr = string.Empty;
    protected string toDateStr = string.Empty;

    protected bool isDateRangeInvalid = false;
    protected string dateRangeErrorMessage = string.Empty;

    protected bool isLoading = true;
    protected bool isLive = false;
    private PeriodicTimer? timer;
    private CancellationTokenSource? cts;

    protected int currentPage = 1;
    protected int pageSize = 15;

    protected bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(searchTerm) ||
        !string.IsNullOrWhiteSpace(selectedLevel) ||
        !string.IsNullOrWhiteSpace(fromDateStr) ||
        !string.IsNullOrWhiteSpace(toDateStr);

    protected List<LogEntry> PagedLogs => allFilteredLogs
        .Skip((currentPage - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    protected bool IsAllCurrentPageSelected =>
        PagedLogs.Any() && PagedLogs.All(log => selectedLogs.Contains(log));

    protected override async Task OnInitializedAsync()
    {
        await LoadLogsAsync(false);
    }

    protected bool ValidateDateRange(out DateTime? fromDate, out DateTime? toDate)
    {
        fromDate = ParsePersianToGregorian(fromDateStr, isEndOfDay: false);
        toDate = ParsePersianToGregorian(toDateStr, isEndOfDay: true);

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            isDateRangeInvalid = true;
            dateRangeErrorMessage = "تاریخ «از تاریخ» نمی‌تواند بزرگتر از «تا تاریخ» باشد. لطفاً بازه انتخابی را اصلاح نمایید.";
            return false;
        }

        isDateRangeInvalid = false;
        dateRangeErrorMessage = string.Empty;
        return true;
    }

    protected async Task LoadLogsAsync(bool isBackgroundRefresh = false)
    {
        if (!ValidateDateRange(out var fromDate, out var toDate))
        {
            if (!isBackgroundRefresh)
            {
                ToastService.ShowWarning(dateRangeErrorMessage);
                isLoading = false;
                StateHasChanged();
            }
            return;
        }

        if (!isBackgroundRefresh)
        {
            isLoading = true;
            StateHasChanged();
        }

        allFilteredLogs = await LogService.GetLogsAsync(fromDate, toDate, selectedLevel, searchTerm);

        if (!isBackgroundRefresh)
        {
            isLoading = false;
        }

        StateHasChanged();
    }

    protected async Task ApplyFilters()
    {
        currentPage = 1;
        expandedRowIndexes.Clear();
        await LoadLogsAsync(false);
    }

    protected async Task ClearFilters()
    {
        searchTerm = string.Empty;
        selectedLevel = string.Empty;
        fromDateStr = string.Empty;
        toDateStr = string.Empty;
        isDateRangeInvalid = false;
        dateRangeErrorMessage = string.Empty;
        currentPage = 1;
        expandedRowIndexes.Clear();
        selectedLogs.Clear();
        await LoadLogsAsync(false);
        ToastService.ShowInfo("تمامی فیلترها پاکسازی شدند.");
    }

    protected void ToggleRowExpand(int index)
    {
        if (expandedRowIndexes.Contains(index))
            expandedRowIndexes.Remove(index);
        else
            expandedRowIndexes.Add(index);
    }

    protected void ToggleSelectLog(LogEntry log, bool isChecked)
    {
        if (isChecked)
            selectedLogs.Add(log);
        else
            selectedLogs.Remove(log);
    }

    protected void ToggleSelectAllOnPage(ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);
        if (isChecked)
        {
            foreach (var log in PagedLogs)
                selectedLogs.Add(log);
        }
        else
        {
            foreach (var log in PagedLogs)
                selectedLogs.Remove(log);
        }
    }

    protected void ClearSelection()
    {
        selectedLogs.Clear();
    }

    protected async Task CopySingleLog(LogEntry log, int rowId)
    {
        var formatted = LogExportFormatter.ToFormattedText(log);
        bool success = await InvokeCopyAsync(formatted);

        if (success)
        {
            recentlyCopiedRowId = rowId;
            ToastService.ShowSuccess("اطلاعات و خطای لاگ با موفقیت کپی شد.");
            StateHasChanged();

            _ = Task.Delay(1500).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    if (recentlyCopiedRowId == rowId)
                    {
                        recentlyCopiedRowId = null;
                        StateHasChanged();
                    }
                });
            });
        }
        else
        {
            ToastService.ShowWarning("امکان دسترسی به کلیپ‌بورد وجود ندارد.");
        }
    }

    protected async Task CopySelectedLogsAsText()
    {
        if (!selectedLogs.Any()) return;

        var formatted = LogExportFormatter.ToBatchFormattedText(selectedLogs);
        bool success = await InvokeCopyAsync(formatted);

        if (success)
        {
            ToastService.ShowSuccess($"اطلاعات {selectedLogs.Count} لاگ انتخاب‌شده به صورت متنی کپی شد.");
        }
        else
        {
            ToastService.ShowWarning("امکان دسترسی به کلیپ‌بورد وجود ندارد.");
        }
    }

    protected async Task CopySelectedLogsAsJson()
    {
        if (!selectedLogs.Any()) return;

        var json = LogExportFormatter.ToJson(selectedLogs);
        bool success = await InvokeCopyAsync(json);

        if (success)
        {
            ToastService.ShowSuccess($"اطلاعات {selectedLogs.Count} لاگ انتخاب‌شده در قالب JSON کپی شد.");
        }
        else
        {
            ToastService.ShowWarning("امکان دسترسی به کلیپ‌بورد وجود ندارد.");
        }
    }

    protected async Task DownloadSelectedLogs()
    {
        if (!selectedLogs.Any()) return;

        var formatted = LogExportFormatter.ToBatchFormattedText(selectedLogs);
        var fileName = $"TicketHub_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

        try
        {
            await JSRuntime.InvokeVoidAsync("downloadFileFromText", fileName, formatted, "text/plain;charset=utf-8");
            ToastService.ShowSuccess($"فایل لاگ‌های انتخابی ({fileName}) دانلود شد.");
        }
        catch
        {
            ToastService.ShowWarning("خطا در ایجاد و دانلود فایل لاگ.");
        }
    }

    private async Task<bool> InvokeCopyAsync(string text)
    {
        try
        {
            return await JSRuntime.InvokeAsync<bool>("copyTextToClipboard", text);
        }
        catch
        {
            return false;
        }
    }

    protected async Task CopyToClipboard(string text)
    {
        bool success = await InvokeCopyAsync(text);
        if (success)
            ToastService.ShowSuccess("متن خطا در کلیپ‌بورد کپی شد.");
        else
            ToastService.ShowWarning("امکان دسترسی به کلیپ‌بورد وجود ندارد.");
    }

    protected async Task ToggleLiveStream()
    {
        isLive = !isLive;

        if (isLive)
        {
            cts = new CancellationTokenSource();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            _ = StartLivePollingAsync(cts.Token);
            ToastService.ShowSuccess("پخش زنده لاگ‌ها فعال شد.");
        }
        else
        {
            cts?.Cancel();
            timer?.Dispose();
            cts = null;
            timer = null;
            ToastService.ShowInfo("پخش زنده لاگ‌ها غیرفعال شد.");
        }
    }

    private async Task StartLivePollingAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (timer != null && await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(async () =>
                {
                    await LoadLogsAsync(isBackgroundRefresh: true);
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Catch intentional cancel
        }
    }

    protected string GetPersianFormattedDate(DateTime dt)
    {
        var pc = new PersianCalendar();
        return $"{pc.GetYear(dt)}/{pc.GetMonth(dt):D2}/{pc.GetDayOfMonth(dt):D2} {dt:HH:mm:ss}";
    }

    private DateTime? ParsePersianToGregorian(string persianDateStr, bool isEndOfDay = false)
    {
        if (string.IsNullOrWhiteSpace(persianDateStr)) return null;

        try
        {
            var normalized = NormalizeDigits(persianDateStr.Trim());
            var parts = normalized.Split(new[] { ' ', 'T' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            var datePart = parts[0];
            var dateSplit = datePart.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (dateSplit.Length != 3)
            {
                if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedGregorian))
                {
                    return isEndOfDay ? parsedGregorian.Date.AddDays(1).AddTicks(-1) : parsedGregorian;
                }
                return null;
            }

            if (!int.TryParse(dateSplit[0], out int year) ||
                !int.TryParse(dateSplit[1], out int month) ||
                !int.TryParse(dateSplit[2], out int day))
            {
                return null;
            }

            if (year < 1300 || year > 1500 || month < 1 || month > 12 || day < 1 || day > 31)
            {
                return null;
            }

            int hour = 0, minute = 0, second = 0, millisecond = 0;

            if (parts.Length > 1)
            {
                var timeSplit = parts[1].Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (timeSplit.Length >= 1) int.TryParse(timeSplit[0], out hour);
                if (timeSplit.Length >= 2) int.TryParse(timeSplit[1], out minute);
                if (timeSplit.Length >= 3) int.TryParse(timeSplit[2], out second);
            }
            else if (isEndOfDay)
            {
                hour = 23;
                minute = 59;
                second = 59;
                millisecond = 999;
            }

            var pc = new PersianCalendar();
            return pc.ToDateTime(year, month, day, hour, minute, second, millisecond);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
    }

    protected async Task SetQuickFilter(string level)
    {
        selectedLevel = level;
        await ApplyFilters();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("matrixRain.init", "matrix-rain-canvas");
            }
            catch { }
        }
    }

    protected string GetLevelBadgeClass(string level) => level?.Trim().ToLowerInvariant() switch
    {
        "fatal" => "bg-red-950/90 text-red-200 border-red-500 shadow-[0_0_12px_rgba(239,68,68,0.6)] animate-pulse",
        "error" => "bg-rose-950/90 text-rose-300 border-rose-500/80 shadow-[0_0_10px_rgba(244,63,94,0.5)]",
        "warning" or "warn" => "bg-amber-950/90 text-amber-300 border-amber-500/80 shadow-[0_0_10px_rgba(245,158,11,0.5)]",
        "information" or "info" => "bg-emerald-950/90 text-emerald-300 border-emerald-500/80 shadow-[0_0_10px_rgba(16,185,129,0.5)]",
        "debug" => "bg-cyan-950/90 text-cyan-300 border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.4)]",
        "verbose" or "trace" => "bg-slate-900/90 text-slate-400 border-slate-700",
        _ => "bg-slate-900/90 text-slate-300 border-slate-700"
    };

    public void Dispose()
    {
        cts?.Cancel();
        timer?.Dispose();
        try
        {
            _ = JSRuntime.InvokeVoidAsync("matrixRain.destroy");
        }
        catch { }
    }

    protected async Task OnFromDateChanged(string val)
    {
        fromDateStr = val;
        if (!ValidateDateRange(out _, out _))
        {
            ToastService.ShowWarning(dateRangeErrorMessage);
            StateHasChanged();
            return;
        }
        await ApplyFilters();
    }

    protected async Task OnToDateChanged(string val)
    {
        toDateStr = val;
        if (!ValidateDateRange(out _, out _))
        {
            ToastService.ShowWarning(dateRangeErrorMessage);
            StateHasChanged();
            return;
        }
        await ApplyFilters();
    }
}
