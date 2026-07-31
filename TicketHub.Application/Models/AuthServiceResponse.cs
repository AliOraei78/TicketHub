namespace TicketHub.Application.Models;

public class AuthServiceResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? Email { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
