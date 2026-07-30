namespace TicketHub.Web.States;

public class UserState
{
    public string SearchTerm { get; set; } = string.Empty;
    public int PageSize { get; set; } = 10;
    public int CurrentPage { get; set; } = 1;
    public bool? SelectedFilterStatus { get; set; } = null;
    public List<int> SelectedFilterRoleIds { get; set; } = new();
    public List<int> SelectedFilterProjectIds { get; set; } = new();
    public HashSet<int> SelectedUserIds { get; set; } = new();

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}
