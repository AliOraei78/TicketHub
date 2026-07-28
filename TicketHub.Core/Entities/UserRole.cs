// TicketHub.Core/Entities/UserRole.cs
namespace TicketHub.Core.Entities
{
    public class UserRole
    {
        public int Id { get; set; } // باید کلید اصلی شود (تغییر در DbContext نیاز است)
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}