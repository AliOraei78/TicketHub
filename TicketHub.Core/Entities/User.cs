// TicketHub.Core/Entities/User.cs
using System.Collections.Generic;

namespace TicketHub.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? PhoneNumber { get; set; }

        // Added for email confirmation mechanism
        public bool IsConfirmed { get; set; } = false;
        public string? ConfirmationToken { get; set; }

        // Navigation Properties
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();   // ← جدید
    }
}