using TicketHub.Application.DTOs;

namespace TicketHub.Application.Models;

public class UserFormSubmissionResult
{
    public UserDto User { get; set; } = new();
    public string Password { get; set; } = string.Empty;
    public List<string> SelectedRoles { get; set; } = new();
}