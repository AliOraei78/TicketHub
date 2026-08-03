namespace TicketHub.Application.DTOs;

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public int? TicketId { get; set; }
    public int? TicketHistoryId { get; set; }
    public int? TransitionFieldId { get; set; }
    public int? TicketFieldValueId { get; set; }
}
