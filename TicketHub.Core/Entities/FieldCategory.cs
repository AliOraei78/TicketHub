using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Entities;

public class FieldCategory
{
    public int Id { get; set; }
    public int TicketFieldId { get; set; }
    public TicketField TicketField { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
