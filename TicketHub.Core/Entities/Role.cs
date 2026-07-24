// TicketHub.Core/Entities/Role.cs
namespace TicketHub.Core.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        // Navigation Property جدید
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<TransitionRole> TransitionRoles { get; set; } = new List<TransitionRole>();
    }
}