// TicketHub.Core/Entities/User.cs
using System.Collections.Generic;
using System.Net.Sockets;

namespace TicketHub.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? PhoneNumber { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}