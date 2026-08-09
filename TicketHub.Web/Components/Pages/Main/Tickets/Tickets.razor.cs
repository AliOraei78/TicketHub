using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

// الان فقط از IDisposable ارث‌بری می‌کند
public partial class Tickets : IDisposable
{
    [Inject] private IState<TicketState> TicketState { get; set; } = default!;
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    // اضافه شدن سابسکرایبر برای گوش دادن به رویدادها
    [Inject] public IActionSubscriber ActionSubscriber { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private bool isCreateModalOpen = false;
    private TicketDto newTicket = new TicketDto();

    private string SearchQuery
    {
        get => TicketState.Value.SearchTerm;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(value, null, null, null, null));
    }

    private int PageSize
    {
        get => TicketState.Value.PageSize;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, value, null, null, null));
    }

    private List<int> SelectedProjectIds
    {
        get => TicketState.Value.SelectedFilterProjectIds;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, null, value, null));
    }

    private List<int> SelectedStatusIds
    {
        get => TicketState.Value.SelectedFilterStatusIds;
        set => Dispatcher.Dispatch(new SetTicketFiltersAction(null, null, null, null, value));
    }

    private IEnumerable<TicketDto> FilteredTickets => TicketState.Value.Tickets;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // گوش دادن به اکشن موفقیت برای بستن خودکار مودال تیکت
        ActionSubscriber.SubscribeToAction<SaveTicketSuccessAction>(this, action =>
        {
            isCreateModalOpen = false;
            InvokeAsync(StateHasChanged);
        });

        // شرط if حذف شد تا با هر بار ورود به صفحه، پروژه‌ها و دسته‌بندی‌های مجاز کاربر واکشی شوند
        var authState = await AuthState;
        var roles = authState.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
    }

    public void Dispose()
    {
        ActionSubscriber.UnsubscribeFromAllActions(this);
    }

    private async Task OpenCreateModal()
    {
        var authState = await AuthState;
        var user = authState.User;
        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        int currentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        newTicket = new TicketDto
        {
            StatusId = 1,
            PriorityId = 2,
            UserId = currentUserId,
            ProjectId = 0
        };

        Dispatcher.Dispatch(new ClearTicketMessagesAction());
        Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        isCreateModalOpen = true;
    }

    private void CloseCreateModal()
    {
        isCreateModalOpen = false;
    }

    private void HandleCreateTicket()
    {
        if (newTicket.ProjectId == 0) return;

        Dispatcher.Dispatch(new SaveTicketAction(newTicket));
        // خط بستن مودال از اینجا حذف شد چون به صورت خودکار در OnInitialized مدیریت می‌شود
    }

    private void NavigateToDetails(int id)
    {
        Navigation.NavigateTo($"/tickets/{id}");
    }

    private void HandleCategoryChanged(int? categoryId)
    {
        newTicket.CategoryId = categoryId;

        if (categoryId.HasValue)
        {
            Dispatcher.Dispatch(new LoadDynamicFieldsAction(categoryId.Value));
        }
        else
        {
            // اگر دسته‌بندی خالی شد، فیلدهای داینامیک هم پاک شوند
            Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        }
    }

    private TicketTransitionModal transitionModal = default!;

    // UI Transient Selection & Delete States
    private HashSet<int> selectedTicketIds = new();
    private bool isDeleteModalOpen = false;
    private string deleteModalDescription = string.Empty;
    private TicketDto? ticketToDelete = null;
    private bool isBulkDelete = false;

    private void ClearSelection() => selectedTicketIds.Clear();

    private void ToggleTicketSelection(int ticketId, bool isSelected)
    {
        if (isSelected)
            selectedTicketIds.Add(ticketId);
        else
            selectedTicketIds.Remove(ticketId);

        selectedTicketIds = new HashSet<int>(selectedTicketIds);
    }

    private void HandleSingleDelete(TicketDto ticket)
    {
        ticketToDelete = ticket;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف تیکت '{ticket.Title}' اطمینان دارید؟";
        isDeleteModalOpen = true;
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        ticketToDelete = null;
        deleteModalDescription = $"آیا از حذف {selectedTicketIds.Count} تیکت انتخاب شده اطمینان دارید؟";
        isDeleteModalOpen = true;
    }

    private void ConfirmDeleteAsync()
    {
        isDeleteModalOpen = false;

        if (isBulkDelete)
        {
            var selectedTitles = TicketState.Value.Tickets
                .Where(t => selectedTicketIds.Contains(t.Id))
                .Select(t => t.Title)
                .ToList();

            Dispatcher.Dispatch(new DeleteMultipleTicketsAction(selectedTicketIds.ToList(), selectedTitles));
            selectedTicketIds.Clear();
        }
        else if (ticketToDelete != null)
        {
            if (selectedTicketIds.Contains(ticketToDelete.Id))
            {
                selectedTicketIds.Remove(ticketToDelete.Id);
                selectedTicketIds = new HashSet<int>(selectedTicketIds);
            }

            Dispatcher.Dispatch(new DeleteTicketAction(ticketToDelete.Id, ticketToDelete.Title));
            ticketToDelete = null;
        }
    }

    private void CancelDelete()
    {
        isDeleteModalOpen = false;
        ticketToDelete = null;
    }

    private async Task HandleActionClick(TicketDto ticket)
    {
        if (ticket.Project != null && ticket.Project.WorkflowId.HasValue)
        {
            await transitionModal.OpenAsync(ticket.Id, ticket.Title, ticket.StatusId, ticket.Project.WorkflowId.Value, ticket.WorkflowStatusId);
        }
    }


    private void HandleTransitionSaved()
    {
        // Reload tickets to reflect the status change
        var authState = AuthState.Result;
        var roles = authState.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
    }
}