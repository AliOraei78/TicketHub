using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class CanvasNodeDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsInitial { get; set; } = false;
    public StatusDto Status { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
}
