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
        public ICollection<RoleProject> RoleProjects { get; set; } = new List<RoleProject>();
        public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
        public ICollection<CategoryRole> CategoryRoles { get; set; } = new List<CategoryRole>();
    }
}