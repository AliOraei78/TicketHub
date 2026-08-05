using System.Text.Json;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Common.Exceptions;

namespace TicketHub.Application.Services;

public class SystemLogService : ISystemLogService
{
    private readonly string _logsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");

    public async Task<List<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? level = null, string? search = null)
    {
        try
        {
            var logs = new List<LogEntry>();
            if (!Directory.Exists(_logsFolderPath)) return logs;

            var logFiles = Directory.GetFiles(_logsFolderPath, "log-*.json");

            foreach (var file in logFiles)
            {
                // استفاده از FileShare.ReadWrite تا هنگام نوشتن لاگ توسط Serilog، فایل قفل نشود
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        // این بخش را پیدا کنید و جایگزین کنید
                        DateTime timestamp = DateTime.MinValue;
                        if (root.TryGetProperty("Timestamp", out var ts) && ts.ValueKind == JsonValueKind.String)
                        {
                            DateTime.TryParse(ts.GetString(), out timestamp);
                        }
                        else if (root.TryGetProperty("@t", out var t) && t.ValueKind == JsonValueKind.String)
                        {
                            DateTime.TryParse(t.GetString(), out timestamp);
                        }

                        // اعمال فیلتر تاریخ در سطح پارس اولیه جهت افزایش سرعت
                        if (fromDate.HasValue && timestamp < fromDate.Value) continue;
                        if (toDate.HasValue && timestamp > toDate.Value) continue;

                        var logLevel = root.TryGetProperty("Level", out var lvl) ? lvl.GetString() :
                                       root.TryGetProperty("@l", out var l) ? l.GetString() : "Information";

                        if (!string.IsNullOrEmpty(level) && !logLevel!.Equals(level, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var message = root.TryGetProperty("RenderedMessage", out var rm) ? rm.GetString() :
                                      root.TryGetProperty("MessageTemplate", out var mt) ? mt.GetString() :
                                      root.TryGetProperty("@m", out var m) ? m.GetString() : string.Empty;

                        if (!string.IsNullOrEmpty(search) && !(message?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            continue;

                        var exception = root.TryGetProperty("Exception", out var ex) ? ex.GetString() :
                                        root.TryGetProperty("@x", out var x) ? x.GetString() : null;

                        string? sourceContext = null;
                        if (root.TryGetProperty("Properties", out var props) && props.TryGetProperty("SourceContext", out var sc))
                        {
                            sourceContext = sc.GetString();
                        }

                        logs.Add(new LogEntry
                        {
                            Timestamp = timestamp,
                            Level = logLevel ?? "Information",
                            Message = message ?? string.Empty,
                            Exception = exception,
                            SourceContext = sourceContext
                        });
                    }
                    catch { /* نادیده گرفتن خطوط غیرمعتبر */ }
                }
            }

            return logs.OrderByDescending(x => x.Timestamp).ToList();
        }

        catch (Exception ex)
        {
            // پرتاب خطای مدیریت شده سیستم
            throw new LogProcessingException($"خطا در خواندن فایل‌های لاگ: {ex.Message}");
        }
    }
}
