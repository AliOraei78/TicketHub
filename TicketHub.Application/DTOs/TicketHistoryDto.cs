using System;
using System.Collections.Generic;

namespace TicketHub.Application.DTOs;

public class TicketHistoryDto
{
    public int Id { get; set; }
    public int? TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public int? TransitionId { get; set; }
    public string TransitionTitle { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public int? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? WorkFlowId { get; set; }
    public string WorkFlowName { get; set; } = string.Empty;
    public int? FromStatusId { get; set; }
    public string? FromStatusName { get; set; }
    public string? FromStatusColor { get; set; }
    public int? ToStatusId { get; set; }
    public string? ToStatusName { get; set; }
    public string? ToStatusColor { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
}
