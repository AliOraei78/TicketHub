using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class PriorityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; }
}
