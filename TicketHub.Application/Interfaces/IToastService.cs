using TicketHub.Application.Models;

namespace TicketHub.Application.Interfaces;

public interface IToastService
{
    event Action? OnChanged;
    IReadOnlyList<ToastMessage> Toasts { get; }

    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
    void RemoveToast(Guid id);
}
