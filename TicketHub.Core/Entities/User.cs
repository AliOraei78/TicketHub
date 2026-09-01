using System.Collections.Generic;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities
{
    public class User : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsConfirmed { get; set; } = false;
        // در این فیلد هشِ کد تایید را ذخیره می‌کنیم
        public string? ConfirmationToken { get; set; }
        // زمان انقضای کد (مثلاً ۱۵ دقیقه)
        public DateTime? TokenExpiration { get; set; }

        // مشخصات احراز هویت یکپارچه و ورود سازمانی (SSO / OIDC)
        public string? ExternalProvider { get; set; }
        public string? ExternalSubjectId { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
    }
}