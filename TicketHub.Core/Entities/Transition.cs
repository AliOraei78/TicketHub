using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Entities;

public class Transition
{
    // --- فیلدهای پایه شما ---
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string SourcePort { get; set; } = "Right"; // Top, Bottom, Right, Left
    public string TargetPort { get; set; } = "Left";

    public int FromState { get; set; }
    public Status? FromStatus { get; set; }

    public int ToState { get; set; }
    public Status? ToStatus { get; set; }

    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;

    public int IsAutomated { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ActivateAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---
    public ICollection<TransitionRole> AllowedRoles { get; set; } = new List<TransitionRole>();
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
}
