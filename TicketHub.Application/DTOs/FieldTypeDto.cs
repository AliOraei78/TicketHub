using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class FieldTypeDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
