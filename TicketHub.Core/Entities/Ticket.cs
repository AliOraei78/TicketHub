// TicketHub.Core/Entities/Ticket.cs
namespace TicketHub.Core.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Medium";   // Low, Medium, High, Critical

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public int? ProjectId { get; set; }

        public User User { get; set; } = null!;
        public Project? Project { get; set; } = null!;
    }
}