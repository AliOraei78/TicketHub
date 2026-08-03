using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Entities;

public class TicketField
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int FieldTypeId { get; set; }
    public FieldType FieldType { get; set; } = null!;
    public ICollection<FieldCategory> FieldCategories { get; set; } = new List<FieldCategory>();
}
