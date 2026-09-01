using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class Tickets : Fluxor.Blazor.Web.Components.FluxorComponent
{
    [Inject] protected IState<TicketState> TicketState { get; set; } = default!;
    [Inject] protected IDispatcher Dispatcher { get; set; } = default!;
    [Inject] protected IActionSubscriber ActionSubscriber { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IPermissionService PermissionService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    protected int CurrentUserId { get; set; } = 0;
    protected bool HasFullAccess { get; set; } = false;

    protected bool IsCreateModalOpen { get; set; } = false;
    protected TicketDto NewTicket { get; set; } = new TicketDto();

    protected string SearchQuery
    {
        get => TicketState.Value.SearchTerm;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(value, null, 1, null, null));
    }

    protected int PageSize
    {
        get => TicketState.Value.PageSize;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, value, 1, null, null));
    }

    protected List<int> SelectedProjectIds
    {
        get => TicketState.Value.SelectedFilterProjectIds;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, 1, value, null));
    }

    protected List<int> SelectedStatusIds
    {
        get => TicketState.Value.SelectedFilterStatusIds;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, 1, null, value, null));
    }

    protected List<int> SelectedPriorityIds
    {
        get => TicketState.Value.SelectedFilterPriorityIds;
        set
        {
            Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, 1, null, null, value));
            Dispatcher.Dispatch(new LoadTicketsAction());
        }
    }

    protected string SortBy
    {
        get => TicketState.Value.SortBy;
        set
        {
            Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, 1, null, null, null, value, null));
            Dispatcher.Dispatch(new LoadTicketsAction());
        }
    }

    protected bool SortIsAscending
    {
        get => TicketState.Value.IsAscending;
        set
        {
            Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, 1, null, null, null, null, value));
            Dispatcher.Dispatch(new LoadTicketsAction());
        }
    }

    protected void ToggleSortDirection()
    {
        SortIsAscending = !SortIsAscending;
    }

    protected List<TicketHub.Web.Components.Shared.SortOption> TicketSortOptions { get; } = new()
    {
        new("createdAt", "📅 تاریخ ثبت تیکت"),
        new("lastAction", "⚡ تاریخ آخرین اقدام"),
        new("priority", "🔥 اولویت تیکت")
    };

    protected IEnumerable<TicketDto> FilteredTickets => TicketState.Value.Tickets;

    protected int TotalPages => TicketState.Value.TotalTickets == 0 ? 1 : (int)Math.Ceiling(TicketState.Value.TotalTickets / (double)TicketState.Value.PageSize);

    protected void NextPage()
    {
        if (TicketState.Value.CurrentPage < TotalPages)
        {
            Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, TicketState.Value.CurrentPage + 1, null, null));
        }
    }

    protected void PreviousPage()
    {
        if (TicketState.Value.CurrentPage > 1)
        {
            Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, TicketState.Value.CurrentPage - 1, null, null));
        }
    }

    protected TicketTransitionModal TransitionModal { get; set; } = default!;

    protected HashSet<int> SelectedTicketIds { get; set; } = new();
    protected bool IsDeleteModalOpen { get; set; } = false;
    protected string DeleteModalDescription { get; set; } = string.Empty;
    protected TicketDto? TicketToDelete { get; set; } = null;
    protected bool IsBulkDelete { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        ActionSubscriber.SubscribeToAction<SaveTicketSuccessAction>(this, action =>
        {
            IsCreateModalOpen = false;
            InvokeAsync(StateHasChanged);
        });

        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = authState.User;
            var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? user.FindFirst("sub")?.Value;
            CurrentUserId = int.TryParse(userIdString, out var id) ? id : 0;

            if (PermissionService != null)
            {
                HasFullAccess = await PermissionService.HasAccessAsync(user, "/tickets", TicketHub.Application.Enums.PermissionType.Full);
            }

            var roles = user.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
        }
    }

    protected override async ValueTask DisposeAsyncCore(bool disposing)
    {
        if (disposing)
        {
            ActionSubscriber.UnsubscribeFromAllActions(this);
        }
        await base.DisposeAsyncCore(disposing);
    }

    protected async Task OpenCreateModal()
    {
        var authState = await AuthState;
        var user = authState.User;
        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        int currentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        NewTicket = new TicketDto
        {
            StatusId = 1,
            PriorityId = 2,
            UserId = currentUserId,
            ProjectId = 0
        };

        Dispatcher.Dispatch(new ClearTicketMessagesAction());
        Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        IsCreateModalOpen = true;
    }

    protected void CloseCreateModal()
    {
        IsCreateModalOpen = false;
    }

    protected void HandleCreateTicket()
    {
        if (NewTicket.ProjectId == 0) return;
        Dispatcher.Dispatch(new SaveTicketAction(NewTicket));
    }

    protected void NavigateToDetails(int id)
    {
        Navigation.NavigateTo($"/tickets/{id}");
    }

    protected void HandleCategoryChanged(int? categoryId)
    {
        NewTicket.CategoryId = categoryId;

        if (categoryId.HasValue)
        {
            Dispatcher.Dispatch(new LoadDynamicFieldsAction(categoryId.Value));
        }
        else
        {
            Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        }
    }

    public void ClearSelection()
    {
        SelectedTicketIds.Clear();
        StateHasChanged();
    }

    public void ToggleTicketSelection(int ticketId, bool isSelected)
    {
        if (isSelected)
            SelectedTicketIds.Add(ticketId);
        else
            SelectedTicketIds.Remove(ticketId);

        SelectedTicketIds = new HashSet<int>(SelectedTicketIds);
        StateHasChanged();
    }

    protected void HandleSingleDelete(TicketDto ticket)
    {
        TicketToDelete = ticket;
        IsBulkDelete = false;
        DeleteModalDescription = $"آیا از حذف تیکت '{ticket.Title}' اطمینان دارید؟";
        IsDeleteModalOpen = true;
        StateHasChanged();
    }

    protected void OpenBulkDeleteModal()
    {
        IsBulkDelete = true;
        TicketToDelete = null;
        DeleteModalDescription = $"آیا از حذف {SelectedTicketIds.Count} تیکت انتخاب شده اطمینان دارید؟";
        IsDeleteModalOpen = true;
        StateHasChanged();
    }

    protected void ConfirmDeleteAsync()
    {
        IsDeleteModalOpen = false;

        if (IsBulkDelete)
        {
            var selectedTitles = TicketState.Value.Tickets
                .Where(t => SelectedTicketIds.Contains(t.Id))
                .Select(t => t.Title)
                .ToList();

            Dispatcher.Dispatch(new DeleteMultipleTicketsAction(SelectedTicketIds.ToList(), selectedTitles));
            SelectedTicketIds.Clear();
        }
        else if (TicketToDelete != null)
        {
            if (SelectedTicketIds.Contains(TicketToDelete.Id))
            {
                SelectedTicketIds.Remove(TicketToDelete.Id);
                SelectedTicketIds = new HashSet<int>(SelectedTicketIds);
            }

            Dispatcher.Dispatch(new DeleteTicketAction(TicketToDelete.Id, TicketToDelete.Title));
            TicketToDelete = null;
        }

        StateHasChanged();
    }

    protected void CancelDelete()
    {
        IsDeleteModalOpen = false;
        TicketToDelete = null;
        StateHasChanged();
    }

    protected async Task HandleActionClick(TicketDto ticket)
    {
        if (ticket.Project != null && ticket.Project.WorkflowId.HasValue)
        {
            await TransitionModal.OpenAsync(ticket.Id, ticket.Title, ticket.StatusId, ticket.Project.WorkflowId.Value, ticket.WorkflowStatusId, ticket.RowVersion);
        }
    }

    protected void HandleTransitionSaved()
    {
        var authState = AuthState.Result;
        var roles = authState.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
    }
}