using System;

namespace TicketHub.Application.DTOs;

public class LookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DailyTrendDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PriorityStatDto
{
    public int PriorityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Count { get; set; }
    public int Percentage { get; set; }
}

public class ProjectWorkloadDto
{
    public string ProjectName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public int Percentage { get; set; }
}

public class TicketTelemetrySummaryDto
{
    public int TotalTickets { get; set; }
    public int NewTicketsCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int CriticalCount { get; set; }
    public int OverdueCount { get; set; }
    public int CriticalAndOverdueCount { get; set; }
    public int SlaOnTimePercentage { get; set; } = 100;
}

public class DashboardSummaryDto
{
    public int TotalTickets { get; set; }
    public int NewTicketsCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int CriticalCount { get; set; }
    public int OverdueCount { get; set; }
    public int CriticalAndOverdueCount { get; set; }
    public int SlaOnTimePercentage { get; set; } = 100;
    public List<DailyTrendDto> TrendData { get; set; } = new();
    public List<PriorityStatDto> PriorityStats { get; set; } = new();
    public List<ProjectWorkloadDto> ProjectStats { get; set; } = new();
    public List<TicketDto> RecentTickets { get; set; } = new();
}

