using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Application.Services;

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
