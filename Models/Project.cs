using System.Collections.Generic;
using System.Net.Sockets;

namespace YourProjectName.Models
{
    public class Project
    {
        // Primary Key
        public int Id { get; set; }

        // Project information
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties for Entity Framework relationships

        // One Project can have many Tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Many-to-Many relationship with Users through UserProject
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}