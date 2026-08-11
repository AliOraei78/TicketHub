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
