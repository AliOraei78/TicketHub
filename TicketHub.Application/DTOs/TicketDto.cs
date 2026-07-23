// TicketSystem.Application/DTOs/TicketDto.cs
using TicketHub.Core.Entities;

namespace TicketSystem.Application.DTOs
{
    public class TicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime CreatedAt { get; set; }

        // Add these lines for the new relation:
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }
}