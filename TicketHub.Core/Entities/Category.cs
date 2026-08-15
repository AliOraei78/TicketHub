using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Category : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<CategoryProject> CategoryProjects { get; set; } = new List<CategoryProject>();
    public ICollection<FieldCategory> FieldCategories { get; set; } = new List<FieldCategory>();
}