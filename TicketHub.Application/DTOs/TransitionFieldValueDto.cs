using System.Collections.Generic;

namespace TicketHub.Application.DTOs;

public class TransitionFieldValueDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int TicketHistoryId { get; set; }
    public int TransitionFieldId { get; set; }
    public bool IsRequired { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public ICollection<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
    public List<FileUploadDto> PendingUploads { get; set; } = new();
}
