using TicketHub.Core.Entities;

namespace TicketHub.Core.Common;

public class CanvasNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Status Status { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
}