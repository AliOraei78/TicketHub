// TicketHub.Core/Entities/Project.cs
using System.Collections.Generic;
using System.Net.Sockets;

namespace TicketHub.Core.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? WorkflowId { get; set; }
        public Workflow? Workflow { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<RoleProject> RoleProjects { get; set; } = new List<RoleProject>();
    }
}