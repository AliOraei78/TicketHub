using TicketHub.Application.Models;

namespace TicketHub.Application.Interfaces;

public interface ISystemLogService
{
    Task<List<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? level = null, string? search = null);
}