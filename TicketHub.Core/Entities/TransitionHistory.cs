using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Core.Entities;

public class TransitionHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int TransitionId { get; set; }
    public Transition Transition { get; set; } = null!;

    public int UserId { get; set; }
    public int? ParentHistoryId { get; set; }
    public string? Comment { get; set; }
    public string? MetaData { get; set; } // فیلد JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public Ticket Ticket { get; set; } = null!;
}