using System;
using System.Collections.Generic;

namespace TicketHub.Core.Entities;

public class TransitionFieldValue
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public int TicketHistoryId { get; set; }
    public TicketHistory TicketHistory { get; set; } = null!;

    public int TransitionFieldId { get; set; }
    public TransitionField TransitionField { get; set; } = null!;

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
