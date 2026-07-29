// TicketHub.Core/Entities/User.cs
using System.Collections.Generic;

namespace TicketHub.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsConfirmed { get; set; } = false;
        // در این فیلد هشِ کد تایید را ذخیره می‌کنیم
        public string? ConfirmationToken { get; set; }
        // زمان انقضای کد (مثلاً ۱۵ دقیقه)
        public DateTime? TokenExpiration { get; set; }
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
    }
}