using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Core.Entities;

public class TicketHistory
{
    public int Id { get; set; }

    public int? TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public Ticket? Ticket { get; set; } = null!;

    public int? TransitionId { get; set; }
    public string TransitionTitle { get; set; } = string.Empty;
    public Transition? Transition { get; set; }

    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public User? User { get; set; } = null!;

    public int? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Role? Role { get; set; } = null!;

    public int? ParentHistoryId { get; set; }
    public TicketHistory? ParentHistory { get; set; }

    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }
    public string? CommentText { get; set; }

    public int? WorkFlowId { get; set; }
    public Workflow? WorkFlow { get; set; }
    public string WorkFlowName { get; set; } = string.Empty;

    // فیلدهای جدید وضعیت مبدا
    public int? FromStatusId { get; set; }
    public Status? FromStatus { get; set; }
    public string? FromStatusName { get; set; }

    // فیلدهای جدید وضعیت مقصد
    public int? ToStatusId { get; set; }
    public Status? ToStatus { get; set; }
    public string? ToStatusName { get; set; }

    public string? MetaData { get; set; } // فیلد JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<TicketHistory> ChildHistories { get; set; } = new List<TicketHistory>();
}