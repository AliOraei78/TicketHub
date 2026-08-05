using TicketHub.Application.Enums;

namespace TicketHub.Application.Models;

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public ToastType Type { get; set; }
}
