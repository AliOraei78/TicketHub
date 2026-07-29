// TicketHub.Core/Entities/RoleProject.cs
namespace TicketHub.Core.Entities
{
    public class RoleProject
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }
}