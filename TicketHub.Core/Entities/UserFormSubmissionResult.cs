using TicketHub.Core.Entities;

public class UserFormSubmissionResult
{
    public User User { get; set; } = new();
    public string Password { get; set; } = string.Empty;
    public List<string> SelectedRoles { get; set; } = new();
    public List<int> SelectedProjectIds { get; set; } = new();
}