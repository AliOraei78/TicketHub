using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class TicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // شناسه‌های ارتباطات (Foreign Keys)
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public int StatusId { get; set; }
    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
    public int? WorkflowStatusId { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value <= DateTime.UtcNow;

    // پروپرتی‌های Navigation (مپ شدن خودکار توسط Mapster و استفاده در UI)
    public UserDto? User { get; set; }
    public ProjectDto? Project { get; set; }
    public StatusDto? Status { get; set; }
    public PriorityDto? Priority { get; set; }
    public CategoryDto? Category { get; set; }
    public WorkflowStatusDto? WorkflowStatus { get; set; }

    // لیست‌های مربوط به ارتباطات چندگانه
    public List<int> AttachmentIds { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<int> TicketHistoryIds { get; set; } = new();
    public List<int> CommentIds { get; set; } = new();
    public List<int> FieldValueIds { get; set; } = new();
    public List<TicketFieldValueDto> FieldValues { get; set; } = new();
}
