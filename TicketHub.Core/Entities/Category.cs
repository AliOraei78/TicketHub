namespace TicketHub.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<CategoryProject> CategoryProjects { get; set; } = new List<CategoryProject>();
    public ICollection<CategoryRole> CategoryRoles { get; set; } = new List<CategoryRole>();
}