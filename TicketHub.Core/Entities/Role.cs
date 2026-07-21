// TicketHub.Core/Entities/Role.cs
namespace TicketHub.Core.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation Property جدید
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}