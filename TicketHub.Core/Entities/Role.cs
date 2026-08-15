using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities
{
    public class Role : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
        // Navigation Property جدید
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<TransitionRole> TransitionRoles { get; set; } = new List<TransitionRole>();
        public ICollection<RoleProject> RoleProjects { get; set; } = new List<RoleProject>();
        public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}