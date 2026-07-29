using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class Tickets : ComponentBase
{
    [Inject] private IRepository<Ticket> TicketRepository { get; set; } = default!;
    [Inject] private IRepository<Project> ProjectRepository { get; set; } = default!;
    [Inject] private IRepository<Status> StatusRepository { get; set; } = default!;

    // مقادیر CascadingParameter و NavigationManager که در بلاک @code داشتید
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;


    private IEnumerable<Ticket> tickets = new List<Ticket>();
    private IEnumerable<Project> projects = new List<Project>();
    private IEnumerable<Status> statuses = new List<Status>();

    private string searchQuery = string.Empty;
    private bool isCreateModalOpen = false;
    private Ticket newTicket = new Ticket();

    // --- متغیرهای فیلتر چندانتخابی ---
    private List<int> selectedProjectIds = new();
    private List<int> selectedStatusIds = new();

    // --- متغیرهای دراپ‌داون ایجاد تیکت ---
    private bool isProjectDropdownOpen = false;
    private string projectSearchQuery = string.Empty;
    private string? selectedProjectName = null;

    // --- متدهای تغییر فیلتر ---
    private void FilterProjectsChanged(List<int> values) => selectedProjectIds = values;
    private void FilterStatusesChanged(List<int> values) => selectedStatusIds = values;

    // --- اعمال فیلترها روی لیست تیکت‌ها ---
    private IEnumerable<Ticket> filteredTickets => tickets
        .Where(t => !selectedStatusIds.Any() || selectedStatusIds.Contains(t.StatusId))
        .Where(t => !selectedProjectIds.Any() || selectedProjectIds.Contains(t.ProjectId))
        .Where(t => string.IsNullOrEmpty(searchQuery) ||
                    t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

    // --- جستجو در دراپ‌داون پروژه‌ها (مدال ایجاد) ---
    private IEnumerable<Project> filteredProjectsForDropdown => projects
        .Where(p => string.IsNullOrEmpty(projectSearchQuery) || p.Name.Contains(projectSearchQuery, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadTickets();
        projects = await ProjectRepository.GetAllAsync();
        statuses = await StatusRepository.GetAllAsync();
    }

    private async Task LoadTickets()
    {
        tickets = await TicketRepository.GetAllWithIncludesAsync(t => t.Project, t => t.Status, t => t.Priority);
    }

    private async Task OpenCreateModal()
    {
        var authState = await AuthState;
        var user = authState.User;
        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int currentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        newTicket = new Ticket
        {
            StatusId = 1,
            PriorityId = 2,
            UserId = currentUserId,
            ProjectId = 0
        };

        selectedProjectName = null;
        projectSearchQuery = string.Empty;
        isProjectDropdownOpen = false;
        isCreateModalOpen = true;
    }

    private void CloseCreateModal()
    {
        isCreateModalOpen = false;
    }

    private void ToggleProjectDropdown()
    {
        isProjectDropdownOpen = !isProjectDropdownOpen;
    }

    private void SelectProject(Project project)
    {
        newTicket.ProjectId = project.Id;
        selectedProjectName = project.Name;
        isProjectDropdownOpen = false;
    }

    private async Task HandleCreateTicket()
    {
        if (newTicket.ProjectId == 0) return;

        newTicket.CreatedAt = DateTime.UtcNow;
        await TicketRepository.AddAsync(newTicket);
        await LoadTickets();
        isCreateModalOpen = false;
    }

    private string GetPriorityClass(string priority) => priority switch
    {
        "Low" => "bg-blue-50 text-blue-600",
        "Medium" => "bg-amber-50 text-amber-600",
        "High" => "bg-orange-50 text-orange-600",
        "Critical" => "bg-rose-50 text-rose-600",
        _ => "bg-gray-50 text-gray-600"
    };

    private string GetStatusClass(string status) => status switch
    {
        "Open" => "bg-emerald-100 text-emerald-800",
        "Closed" => "bg-gray-100 text-gray-600",
        _ => "bg-gray-100 text-gray-600"
    };

    private void NavigateToDetails(int id)
    {
        Navigation.NavigateTo($"/tickets/{id}");
    }
}
