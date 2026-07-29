using TicketHub.Application.DTOs;

namespace TicketHub.Web.State;

public class CategoryState
{
    public List<CategoryDto> Categories { get; set; } = new();
    public bool IsLoading { get; set; } = false;

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}