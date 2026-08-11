using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Common.Exceptions;

namespace TicketHub.Application.Services;

public class SystemLogService : ISystemLogService
{
    private readonly string _logsFolderPath;
    private static readonly Regex LogFileDateRegex = new(@"log-(\d{4})(\d{2})(\d{2})", RegexOptions.Compiled);
    private static readonly Regex TokenPlaceholderRegex = new(@"\{([@$]?[a-zA-Z0-9_]+)(?::[^}]+)?\}", RegexOptions.Compiled);

    public SystemLogService(IHostEnvironment? hostEnvironment = null)
    {
        if (hostEnvironment != null && !string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath))
        {
            var primaryPath = Path.Combine(hostEnvironment.ContentRootPath, "logs");
            if (Directory.Exists(primaryPath))
            {
                _logsFolderPath = primaryPath;
                return;
            }
        }

        var currentDirLogs = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (Directory.Exists(currentDirLogs))
        {
            _logsFolderPath = currentDirLogs;
            return;
        }

        var baseDirLogs = Path.Combine(AppContext.BaseDirectory, "logs");
        if (Directory.Exists(baseDirLogs))
        {
            _logsFolderPath = baseDirLogs;
            return;
        }

        _logsFolderPath = hostEnvironment != null && !string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
            ? Path.Combine(hostEnvironment.ContentRootPath, "logs")
            : currentDirLogs;
    }

    public SystemLogService(string customLogsPath)
    {
        _logsFolderPath = customLogsPath;
    }

    public async Task<List<LogEntry>> GetLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? level = null,
        string? search = null)
    {
        try
        {
            var logs = new List<LogEntry>();
            if (!Directory.Exists(_logsFolderPath)) return logs;

            var logFilePaths = Directory.GetFiles(_logsFolderPath, "log-*.json");
            if (logFilePaths.Length == 0) return logs;

            // فیلتر کردن فایل‌ها بر اساس تاریخ موجود در نام فایل جهت جلوگیری از خواندن فایل‌های خارج از بازه
            var candidateFiles = FilterFilesByDateRange(logFilePaths, fromDate, toDate);

            // مرتب‌سازی فایل‌ها از جدیدترین به قدیمی‌ترین
            candidateFiles = candidateFiles.OrderByDescending(f => f).ToList();

            var searchTrimmed = search?.Trim();
            var levelTrimmed = level?.Trim();

            foreach (var file in candidateFiles)
            {
                // استفاده از FileShare.ReadWrite برای خواندن بدون قفل کردن نوشتن فعال Serilog
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

                        // استخراج تاریخ و زمان
                        DateTime timestamp = DateTime.MinValue;
                        if (root.TryGetProperty("Timestamp", out var ts) && ts.ValueKind == JsonValueKind.String)
                        {
                            if (DateTimeOffset.TryParse(ts.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                            {
                                timestamp = dto.LocalDateTime;
                            }
                        }
                        else if (root.TryGetProperty("@t", out var t) && t.ValueKind == JsonValueKind.String)
                        {
                            if (DateTimeOffset.TryParse(t.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                            {
                                timestamp = dto.LocalDateTime;
                            }
                        }

                        // اعمال فیلتر تاریخ در سطح پردازش خط
                        if (fromDate.HasValue && timestamp < fromDate.Value) continue;
                        if (toDate.HasValue && timestamp > toDate.Value) continue;

                        // استخراج سطح لاگ
                        var logLevel = root.TryGetProperty("Level", out var lvl) ? lvl.GetString() :
                                       root.TryGetProperty("@l", out var l) ? l.GetString() : "Information";

                        if (string.IsNullOrWhiteSpace(logLevel)) logLevel = "Information";

                        if (!string.IsNullOrEmpty(levelTrimmed) && !logLevel.Equals(levelTrimmed, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // استخراج استک تریس خطا
                        var exception = root.TryGetProperty("Exception", out var ex) ? ex.GetString() :
                                        root.TryGetProperty("@x", out var x) ? x.GetString() : null;

                        // استخراج ردیاب‌ها (TraceId, SpanId)
                        string? traceId = root.TryGetProperty("TraceId", out var trId) ? trId.GetString() : null;
                        string? spanId = root.TryGetProperty("SpanId", out var spId) ? spId.GetString() : null;

                        // استخراج دیکشنری Properties
                        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        string? sourceContext = null;
                        string? correlationId = null;

                        if (root.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in props.EnumerateObject())
                            {
                                var valStr = prop.Value.ValueKind switch
                                {
                                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                                    JsonValueKind.Null => string.Empty,
                                    _ => prop.Value.GetRawText()
                                };

                                properties[prop.Name] = valStr;

                                if (prop.Name.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                                    sourceContext = valStr;
                                else if (prop.Name.Equals("CorrelationId", StringComparison.OrdinalIgnoreCase))
                                    correlationId = valStr;
                            }
                        }

                        // استخراج پیام رندر شده یا قالب پیام و رندر پارامترهای آن
                        string message;
                        if (root.TryGetProperty("RenderedMessage", out var rm) && !string.IsNullOrWhiteSpace(rm.GetString()))
                        {
                            message = rm.GetString()!;
                        }
                        else if (root.TryGetProperty("@m", out var m) && !string.IsNullOrWhiteSpace(m.GetString()))
                        {
                            message = m.GetString()!;
                        }
                        else if (root.TryGetProperty("MessageTemplate", out var mt) && !string.IsNullOrWhiteSpace(mt.GetString()))
                        {
                            message = RenderMessageTemplate(mt.GetString()!, properties, root);
                        }
                        else if (root.TryGetProperty("@mt", out var atMt) && !string.IsNullOrWhiteSpace(atMt.GetString()))
                        {
                            message = RenderMessageTemplate(atMt.GetString()!, properties, root);
                        }
                        else
                        {
                            message = string.Empty;
                        }

                        // اعمال فیلتر جستجوی جامع (در پیام، استک خطا، منبع و مقادیر متادیتا)
                        if (!string.IsNullOrEmpty(searchTrimmed))
                        {
                            bool matches = MatchesSearch(searchTrimmed, message, exception, sourceContext, traceId, correlationId, properties);
                            if (!matches) continue;
                        }

                        logs.Add(new LogEntry
                        {
                            Timestamp = timestamp,
                            Level = logLevel,
                            Message = message,
                            Exception = exception,
                            SourceContext = sourceContext,
                            TraceId = traceId,
                            SpanId = spanId,
                            CorrelationId = correlationId,
                            Properties = properties
                        });
                    }
                    catch
                    {
                        // نادیده گرفتن خطوط ناقص هنگام نوشتن همزمان
                    }
                }
            }

            return logs.OrderByDescending(x => x.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            throw new LogProcessingException($"خطا در خواندن فایل‌های لاگ: {ex.Message}");
        }
    }

    private static List<string> FilterFilesByDateRange(string[] filePaths, DateTime? fromDate, DateTime? toDate)
    {
        var result = new List<string>();

        foreach (var path in filePaths)
        {
            var fileName = Path.GetFileName(path);
            var match = LogFileDateRegex.Match(fileName);

            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out int year) &&
                int.TryParse(match.Groups[2].Value, out int month) &&
                int.TryParse(match.Groups[3].Value, out int day))
            {
                var fileDate = new DateOnly(year, month, day);

                if (fromDate.HasValue)
                {
                    var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                    if (fileDate < fromDateOnly) continue;
                }

                if (toDate.HasValue)
                {
                    var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                    if (fileDate > toDateOnly) continue;
                }
            }

            result.Add(path);
        }

        return result;
    }

    private static string RenderMessageTemplate(string template, Dictionary<string, string> properties, JsonElement root)
    {
        if (string.IsNullOrEmpty(template) || !template.Contains('{')) return template;

        // بررسی وجود بخش Renderings در خروجی Serilog
        Dictionary<string, string>? renderings = null;
        if (root.TryGetProperty("Renderings", out var rendObj) && rendObj.ValueKind == JsonValueKind.Object)
        {
            renderings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rProp in rendObj.EnumerateObject())
            {
                if (rProp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in rProp.Value.EnumerateArray())
                    {
                        if (item.TryGetProperty("Rendering", out var rVal))
                        {
                            renderings[rProp.Name] = rVal.GetString() ?? string.Empty;
                            break;
                        }
                    }
                }
            }
        }

        return TokenPlaceholderRegex.Replace(template, match =>
        {
            var tokenName = match.Groups[1].Value.TrimStart('@', '$');

            if (renderings != null && renderings.TryGetValue(tokenName, out var renderedVal))
                return renderedVal;

            if (properties.TryGetValue(tokenName, out var propVal))
                return propVal;

            return match.Value;
        });
    }

    private static bool MatchesSearch(
        string query,
        string message,
        string? exception,
        string? sourceContext,
        string? traceId,
        string? correlationId,
        Dictionary<string, string> properties)
    {
        if (message.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(exception) && exception.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(sourceContext) && sourceContext.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(traceId) && traceId.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(correlationId) && correlationId.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var kvp in properties)
        {
            if (kvp.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                kvp.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

