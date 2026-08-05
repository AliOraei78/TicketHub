using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;

namespace TicketHub.Application.Services;
public class ToastService : IToastService
{
    public event Action? OnChanged;
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public void ShowSuccess(string message, string? title = null) => AddToast(message, title, ToastType.Success);
    public void ShowError(string message, string? title = null) => AddToast(message, title, ToastType.Error);
    public void ShowWarning(string message, string? title = null) => AddToast(message, title, ToastType.Warning);
    public void ShowInfo(string message, string? title = null) => AddToast(message, title, ToastType.Info);

    private void AddToast(string message, string? title, ToastType type)
    {
        var toast = new ToastMessage { Message = message, Title = title, Type = type };
        _toasts.Add(toast);
        OnChanged?.Invoke();

        // حذف خودکار بعد از 5 ثانیه
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            if (Toasts.Contains(toast))
            {
                _toasts.Remove(toast);
                OnChanged?.Invoke();
            }
        });
    }

    public void RemoveToast(Guid id)
    {
        _toasts.RemoveAll(x => x.Id == id);
        OnChanged?.Invoke();
    }
}
