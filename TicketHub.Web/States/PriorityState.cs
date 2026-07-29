using TicketHub.Core.Entities;

namespace TicketHub.Web.States;

public class PriorityState
{
    public List<Priority>? Priorities { get; private set; }
    public bool IsLoading { get; private set; } = true;
    public string? StatusMessage { get; private set; }
    public bool IsError { get; private set; }

    public event Action? OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();

    public void SetPriorities(List<Priority> priorities)
    {
        Priorities = priorities;
        IsLoading = false;
        NotifyStateChanged();
    }

    public void SetMessage(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
        NotifyStateChanged();
    }
}