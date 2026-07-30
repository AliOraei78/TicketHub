using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class WorkflowStatusDto
{
    public int Id { get; set; }
    public Guid NodeId { get; set; }
    public int WorkflowId { get; set; }
    public int StatusId { get; set; }

    public StatusDto? Status { get; set; }

    public double PositionX { get; set; }
    public double PositionY { get; set; }
}
