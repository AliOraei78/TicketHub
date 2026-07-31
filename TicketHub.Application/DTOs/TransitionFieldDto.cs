using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class TransitionFieldDto
{
    public int Id { get; set; }

    public int TransitionId { get; set; }
    public int FieldTypeId { get; set; }

    public string FieldName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;

    // اگر AttachmentDto داری از خط زیر استفاده کن، در غیر این صورت پاکش کن
    // public ICollection<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
}
