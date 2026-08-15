using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Attachment : ISoftDeletable
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    // ارتباط با تیکت (اگر فایل مستقیم روی تیکت آپلود شود)
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // ارتباط با تاریخچه انتقال (اگر فایل هنگام Transition آپلود شود)
    public int? TicketHistoryId { get; set; }
    public TicketHistory? TicketHistory { get; set; }

    // ارتباط با فیلدهای انتقال یا فیلدهای تیکت
    public int? TransitionFieldId { get; set; }
    public TransitionField? TransitionField { get; set; }
    public int? TicketFieldValueId { get; set; }
    public TicketFieldValue? TicketFieldValue { get; set; }
    public int? TransitionFieldValueId { get; set; }
    public TransitionFieldValue? TransitionFieldValue { get; set; }
}