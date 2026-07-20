namespace YourProjectName.Models
{
    // Junction table for Many-to-Many relationship between User and Project
    public class UserProject
    {
        // Foreign Keys
        public int UserId { get; set; }
        public int ProjectId { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }
}