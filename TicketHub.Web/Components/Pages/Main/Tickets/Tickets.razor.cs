using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class Tickets : ComponentBase
{
    [Inject] private ITicketService TicketService { get; set; } = default!;
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IStatusService StatusService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private int pageSize = 10;
    private IEnumerable<TicketDto> tickets = new List<TicketDto>();
    private IEnumerable<ProjectDto> projects = new List<ProjectDto>();
    private IEnumerable<StatusDto> statuses = new List<StatusDto>();

    private string searchQuery = string.Empty;
    private bool isCreateModalOpen = false;
    private TicketDto newTicket = new TicketDto();

    private List<int> selectedProjectIds = new();
    private List<int> selectedStatusIds = new();

    private IEnumerable<TicketDto> filteredTickets => tickets
        .Where(t => !selectedStatusIds.Any() || selectedStatusIds.Contains(t.StatusId))
        .Where(t => !selectedProjectIds.Any() || selectedProjectIds.Contains(t.ProjectId))
        .Where(t => string.IsNullOrEmpty(searchQuery) ||
                    t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadTickets();
        projects = await ProjectService.GetProjectsAsync();
        statuses = await StatusService.GetAllAsync();
    }

    private async Task LoadTickets()
    {
        var result = await TicketService.GetFilteredTicketsAsync(string.Empty, null, null, null, 1, 1000);
        tickets = result.Tickets;
    }

    private async Task OpenCreateModal()
    {
        var authState = await AuthState;
        var user = authState.User;
        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int currentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        newTicket = new TicketDto
        {
            StatusId = 1,
            PriorityId = 2,
            UserId = currentUserId,
            ProjectId = 0
        };

        isCreateModalOpen = true;
    }

    private void CloseCreateModal()
    {
        isCreateModalOpen = false;
    }

    private async Task HandleCreateTicket()
    {
        if (newTicket.ProjectId == 0) return;

        await TicketService.CreateAsync(newTicket);
        await LoadTickets();
        isCreateModalOpen = false;
    }

    private void NavigateToDetails(int id)
    {
        Navigation.NavigateTo($"/tickets/{id}");
    }
}