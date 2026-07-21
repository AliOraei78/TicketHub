namespace TicketHub.Core.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation property if needed for future relations
        // public ICollection<User> Users { get; set; } = new List<User>();
    }
}