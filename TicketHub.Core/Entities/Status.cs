// TicketHub.Core/Entities/Status.cs
namespace TicketHub.Core.Entities
{
    public class Status
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Hex color code for color picker (e.g. #FF0000)
        public string ColorCode { get; set; } = "#3B82F6";
        public bool NeedApproval { get; set; } = false;

        // 1-to-many relationship: One status can have many tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}