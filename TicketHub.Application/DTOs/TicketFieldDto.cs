using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class TicketFieldDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FieldTypeId { get; set; }
    public FieldTypeDto? FieldType { get; set; }
    public List<int> CategoryIds { get; set; } = new();
}
