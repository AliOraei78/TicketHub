using System;
using System.IO;
using System.Threading.Tasks;
using TicketHub.Application.Services;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class SystemLogServiceTests : IDisposable
    {
        private readonly string _tempLogsDir;

        public SystemLogServiceTests()
        {
            _tempLogsDir = Path.Combine(Path.GetTempPath(), "TicketHub_TestLogs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempLogsDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempLogsDir))
            {
                try { Directory.Delete(_tempLogsDir, true); } catch { }
            }
        }

        [Fact]
        public async Task GetLogsAsync_DirectoryDoesNotExist_ReturnsEmptyList()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N"));
            var service = new SystemLogService(nonExistentPath);

            var result = await service.GetLogsAsync();

            result.Should().NotBeNull();            result.Should().BeEmpty();        }

        [Fact]
        public async Task GetLogsAsync_RendersTemplatePlaceholders_WithPropertyValues()
        {
            var logFilePath = Path.Combine(_tempLogsDir, "log-20260811.json");
            var jsonLine = "{\"Timestamp\":\"2026-08-11T10:15:30.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms\",\"TraceId\":\"trace-123\",\"SpanId\":\"span-456\",\"Properties\":{\"RequestMethod\":\"GET\",\"RequestPath\":\"/api/tickets\",\"StatusCode\":200,\"Elapsed\":15.42,\"SourceContext\":\"TicketController\",\"CorrelationId\":\"corr-789\"}}";
            await File.WriteAllTextAsync(logFilePath, jsonLine + Environment.NewLine);

            var service = new SystemLogService(_tempLogsDir);
            var logs = await service.GetLogsAsync();

            logs.Should().ContainSingle();            var entry = logs[0];
            entry.Level.Should().Be("Information");            entry.Message.Should().Be("HTTP GET /api/tickets responded 200 in 15.42 ms");            entry.TraceId.Should().Be("trace-123");            entry.SpanId.Should().Be("span-456");            entry.CorrelationId.Should().Be("corr-789");            entry.SourceContext.Should().Be("TicketController");            entry.Properties["RequestMethod"].Should().Be("GET");            entry.Properties["StatusCode"].Should().Be("200");        }

        [Fact]
        public async Task GetLogsAsync_FiltersByLogLevel()
        {
            var logFilePath = Path.Combine(_tempLogsDir, "log-20260811.json");
            var line1 = "{\"Timestamp\":\"2026-08-11T10:00:00.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"Info message\"}";
            var line2 = "{\"Timestamp\":\"2026-08-11T10:05:00.0000000+03:30\",\"Level\":\"Error\",\"MessageTemplate\":\"Error message\",\"Exception\":\"System.Exception: failed\"}";
            var line3 = "{\"Timestamp\":\"2026-08-11T10:10:00.0000000+03:30\",\"Level\":\"Warning\",\"MessageTemplate\":\"Warning message\"}";
            await File.WriteAllLinesAsync(logFilePath, new[] { line1, line2, line3 });

            var service = new SystemLogService(_tempLogsDir);
            var errorLogs = await service.GetLogsAsync(level: "Error");

            errorLogs.Should().ContainSingle();            errorLogs[0].Level.Should().Be("Error");            errorLogs[0].Message.Should().Be("Error message");            errorLogs[0].Exception.Should().Be("System.Exception: failed");        }

        [Fact]
        public async Task GetLogsAsync_SearchesAcrossMessageExceptionAndProperties()
        {
            var logFilePath = Path.Combine(_tempLogsDir, "log-20260811.json");
            var line1 = "{\"Timestamp\":\"2026-08-11T10:00:00.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"User logged in\",\"Properties\":{\"UserId\":\"User_100\"}}";
            var line2 = "{\"Timestamp\":\"2026-08-11T10:05:00.0000000+03:30\",\"Level\":\"Error\",\"MessageTemplate\":\"DB Operation failed\",\"Exception\":\"Microsoft.Data.SqlClient.SqlException: Timeout\"}";
            await File.WriteAllLinesAsync(logFilePath, new[] { line1, line2 });

            var service = new SystemLogService(_tempLogsDir);

            // Search in properties
            var propertySearchResult = await service.GetLogsAsync(search: "User_100");
            propertySearchResult.Should().ContainSingle();            propertySearchResult[0].Message.Should().Be("User logged in");
            // Search in exception
            var exceptionSearchResult = await service.GetLogsAsync(search: "SqlException");
            exceptionSearchResult.Should().ContainSingle();            exceptionSearchResult[0].Message.Should().Be("DB Operation failed");        }

        [Fact]
        public async Task GetLogsAsync_FiltersByDateRange_PruningFilesOutsideRange()
        {
            var fileDay1 = Path.Combine(_tempLogsDir, "log-20260809.json");
            var fileDay2 = Path.Combine(_tempLogsDir, "log-20260810.json");
            var fileDay3 = Path.Combine(_tempLogsDir, "log-20260811.json");

            await File.WriteAllTextAsync(fileDay1, "{\"Timestamp\":\"2026-08-09T10:00:00.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"Day 9 log\"}\n");
            await File.WriteAllTextAsync(fileDay2, "{\"Timestamp\":\"2026-08-10T10:00:00.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"Day 10 log\"}\n");
            await File.WriteAllTextAsync(fileDay3, "{\"Timestamp\":\"2026-08-11T10:00:00.0000000+03:30\",\"Level\":\"Information\",\"MessageTemplate\":\"Day 11 log\"}\n");

            var service = new SystemLogService(_tempLogsDir);

            var fromDate = new DateTime(2026, 8, 10, 0, 0, 0);
            var toDate = new DateTime(2026, 8, 10, 23, 59, 59);

            var result = await service.GetLogsAsync(fromDate: fromDate, toDate: toDate);

            result.Should().ContainSingle();            result[0].Message.Should().Be("Day 10 log");        }

        [Fact]
        public void LogExportFormatter_ToFormattedText_FormatsLogCorrectly()
        {
            var log = new TicketHub.Application.Models.LogEntry
            {
                Timestamp = new DateTime(2026, 8, 11, 14, 30, 0),
                Level = "Error",
                Message = "Database operation timed out",
                SourceContext = "TicketService",
                TraceId = "trace-abc",
                SpanId = "span-def",
                CorrelationId = "corr-ghi",
                Exception = "Microsoft.Data.SqlClient.SqlException: Timeout",
                Properties = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "UserId", "42" },
                    { "StatusCode", "500" }
                }
            };

            var formatted = TicketHub.Application.Common.LogExportFormatter.ToFormattedText(log);

            formatted.Should().Contain("[ERROR]");            formatted.Should().Contain("TicketService");            formatted.Should().Contain("Database operation timed out");            formatted.Should().Contain("TraceId=trace-abc");            formatted.Should().Contain("SpanId=span-def");            formatted.Should().Contain("CorrelationId=corr-ghi");            formatted.Should().Contain("UserId: 42");            formatted.Should().Contain("SqlException: Timeout");        }

        [Fact]
        public void LogExportFormatter_ToBatchFormattedText_And_ToJson_WorkCorrectly()
        {
            var logs = new[]
            {
                new TicketHub.Application.Models.LogEntry { Timestamp = DateTime.Now, Level = "Information", Message = "Log 1" },
                new TicketHub.Application.Models.LogEntry { Timestamp = DateTime.Now, Level = "Warning", Message = "Log 2" }
            };

            var batchText = TicketHub.Application.Common.LogExportFormatter.ToBatchFormattedText(logs);
            batchText.Should().Contain("تعداد لاگ‌ها: 2");            batchText.Should().Contain("Log 1");            batchText.Should().Contain("Log 2");
            var json = TicketHub.Application.Common.LogExportFormatter.ToJson(logs);
            json.Should().Contain("Log 1");            json.Should().Contain("Log 2");            json.Should().Contain("Information");        }
    }
}

