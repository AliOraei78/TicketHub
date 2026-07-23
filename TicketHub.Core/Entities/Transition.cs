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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // --- Navigation Properties ---
    public ICollection<TransitionRole> AllowedRoles { get; set; } = new List<TransitionRole>();
    public ICollection<TransitionHistory> Histories { get; set; } = new List<TransitionHistory>();
}
