using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class TicketField : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Options { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }
    public int FieldTypeId { get; set; }
    public FieldType FieldType { get; set; } = null!;
    public ICollection<FieldCategory> FieldCategories { get; set; } = new List<FieldCategory>();
    public ICollection<TicketFieldValue> TicketFieldValues { get; set; } = new List<TicketFieldValue>();
}
