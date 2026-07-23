// TicketHub.Core/Entities/UserProject.cs
namespace TicketHub.Core.Entities
{
    public class UserProject
    {
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }
}