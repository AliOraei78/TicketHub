// TicketHub.Application/DTOs/TicketDto.cs
using TicketHub.Core.Entities;

namespace TicketHub.Application.DTOs;

public class TicketListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Mapster خودکار این‌ها را از Navigation Propertyها پر می‌کند
    public string UserName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusColorCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
}

public class TicketCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int StatusId { get; set; }
    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
}