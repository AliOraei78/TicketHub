using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using TicketHub.Application.Models;

namespace TicketHub.Application.Common;

public static class LogExportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string ToFormattedText(LogEntry log)
    {
        var sb = new StringBuilder();
        var pc = new PersianCalendar();
        var jalaliDate = $"{pc.GetYear(log.Timestamp):D4}/{pc.GetMonth(log.Timestamp):D2}/{pc.GetDayOfMonth(log.Timestamp):D2} {log.Timestamp:HH:mm:ss.fff}";

        sb.AppendLine("======================================================================");
        sb.AppendLine($"[{jalaliDate}] [{log.Level.ToUpperInvariant()}] {(string.IsNullOrWhiteSpace(log.SourceContext) ? "System" : log.SourceContext)}");
        sb.AppendLine($"پیام: {log.Message}");

        var hasTrace = !string.IsNullOrEmpty(log.TraceId) || !string.IsNullOrEmpty(log.SpanId) || !string.IsNullOrEmpty(log.CorrelationId);
        if (hasTrace)
        {
            var traceParts = new List<string>();
            if (!string.IsNullOrEmpty(log.CorrelationId)) traceParts.Add($"CorrelationId={log.CorrelationId}");
            if (!string.IsNullOrEmpty(log.TraceId)) traceParts.Add($"TraceId={log.TraceId}");
            if (!string.IsNullOrEmpty(log.SpanId)) traceParts.Add($"SpanId={log.SpanId}");
            sb.AppendLine($"شناسه رهگیری: {string.Join(" | ", traceParts)}");
        }

        if (log.Properties != null && log.Properties.Count > 0)
        {
            sb.AppendLine("مشخصات و متادیتا (Properties):");
            foreach (var kvp in log.Properties)
            {
                sb.AppendLine($"  • {kvp.Key}: {kvp.Value}");
            }
        }

        if (!string.IsNullOrEmpty(log.Exception))
        {
            sb.AppendLine("----------------------------------------------------------------------");
            sb.AppendLine("استک تریس و جزئیات خطا (Exception):");
            sb.AppendLine(log.Exception.Trim());
        }

        sb.AppendLine("======================================================================");
        return sb.ToString();
    }

    public static string ToBatchFormattedText(IEnumerable<LogEntry> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# خروجی گزارش لاگ‌های سیستم ({DateTime.Now:yyyy/MM/dd HH:mm:ss})");
        sb.AppendLine($"# تعداد لاگ‌ها: {logs.Count()}");
        sb.AppendLine();

        foreach (var log in logs)
        {
            sb.AppendLine(ToFormattedText(log));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string ToJson(IEnumerable<LogEntry> logs)
    {
        return JsonSerializer.Serialize(logs, JsonOptions);
    }

    public static string ToJson(LogEntry log)
    {
        return JsonSerializer.Serialize(log, JsonOptions);
    }
}
