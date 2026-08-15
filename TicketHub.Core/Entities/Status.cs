using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities
{
    public class Status : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // Hex color code for color picker (e.g. #FF0000)
        public string ColorCode { get; set; } = "#3B82F6";
        public bool NeedApproval { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
        // 1-to-many relationship: One status can have many tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        // روابط جدید اضافه شده برای ترنزیشن‌ها (حذف شده)
        
        // این خطوط اضافه شوند
        public ICollection<TicketHistory> FromHistories { get; set; } = new List<TicketHistory>();
        public ICollection<TicketHistory> ToHistories { get; set; } = new List<TicketHistory>();
        public ICollection<WorkflowStatus> WorkflowStatuses { get; set; } = new List<WorkflowStatus>();
    }
}