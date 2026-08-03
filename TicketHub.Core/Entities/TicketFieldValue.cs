using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Entities;

public class TicketFieldValue
{
    public int Id { get; set; }

    // ذخیره مقادیر به صورت String/JSON تا هر نوع داده‌ای را پشتیبانی کند
    public string Value { get; set; } = string.Empty;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int TicketFieldId { get; set; }
    public TicketField TicketField { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}