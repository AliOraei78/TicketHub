using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class TicketFieldValueDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public int TicketFieldId { get; set; }
    public bool IsRequired { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public ICollection<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
    public List<FileUploadDto> PendingUploads { get; set; } = new();
}
