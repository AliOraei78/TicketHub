using TicketHub.Application.DTOs;

namespace TicketHub.Web.States;

public class StatusState
{
    public List<StatusDto>? Statuses { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public HashSet<int> SelectedStatusIds { get; set; } = new();

    public event Action? OnStateChange;
    public void NotifyStateChanged() => OnStateChange?.Invoke();
}
