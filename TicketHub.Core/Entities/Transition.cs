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
    public int FromState { get; set; }
    public int ToState { get; set; }
    public int IsAutomated { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }
    // --- Navigation Properties ---
    public ICollection<TransitionRole> AllowedRoles { get; set; } = new List<TransitionRole>();
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
}
