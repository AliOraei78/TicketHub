using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace YourProjectName.Models
{
    public class User
    {
        // Primary Key
        public int Id { get; set; }

        // Basic user information
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // In real app, this should be hashed
        public string Role { get; set; } = "User"; // e.g., Admin, User, etc.
        public string? PhoneNumber { get; set; }

        // Navigation Properties for Entity Framework relationships

        // One User can create many Tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Many-to-Many relationship with Projects through UserProject
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}