using System;
using System.Collections.Generic;

namespace TicketHub.Application.DTOs;

public class DynamicFieldModel
{
    public int OriginalFieldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public int FieldTypeId { get; set; }
    
    // Bound value for UI
    public string Value { get; set; } = string.Empty;
    public List<FileUploadDto> PendingUploads { get; set; } = new();
}
