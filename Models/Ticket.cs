using System;

namespace YourProjectName.Models
{
    public class Ticket
    {
        // Primary Key
        public int Id { get; set; }

        // Ticket information
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // e.g., Open, InProgress, Closed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int UserId { get; set; }
        public int ProjectId { get; set; }

        // Navigation Properties for Entity Framework relationships

        // Each Ticket belongs to one User (Creator)
        public User User { get; set; } = null!;

        // Each Ticket belongs to one Project
        public Project Project { get; set; } = null!;
    }
}