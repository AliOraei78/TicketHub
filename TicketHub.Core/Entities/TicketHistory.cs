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
    public int TicketId { get; set; }
    public int TransitionId { get; set; }
    public Transition Transition { get; set; } = null!;
    public int UserId { get; set; }
    public int? ParentHistoryId { get; set; }
    public int? CommentId { get; set; }
    public Comment? Comment { get; set; } // این خط اضافه شود
    public int? WorkFlowId { get; set; }
    public Workflow? WorkFlow { get; set; } // این خط اضافه شود
    public string? MetaData { get; set; } // فیلد JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public Ticket Ticket { get; set; } = null!;
}