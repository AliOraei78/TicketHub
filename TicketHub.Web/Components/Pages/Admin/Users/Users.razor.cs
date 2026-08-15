using Fluxor;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Users;

public partial class Users
{
    [Inject] public IState<UserState> UsrState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;
    [Inject] public IActionSubscriber ActionSubscriber { get; set; } = default!;

    // UI Variables
    private bool isUserModalOpen = false;
    private UserDto userModel = new();
    private string passwordInput = string.Empty;
    private List<string> selectedRoles = new();

    private bool showDeleteModal = false;
    private UserDto? userToDelete;
    private HashSet<int> selectedUserIds = new();
    private HashSet<int> deletingUserIds = new();
    private bool isBulkDelete = false;

    // Telemetry
    private int TotalUsersCount => UsrState.Value.MasterTotalUsers > 0 ? UsrState.Value.MasterTotalUsers : UsrState.Value.TotalUsers;
    private int ActiveUsersCount => (UsrState.Value.MasterActiveUsers > 0 || UsrState.Value.MasterInactiveUsers > 0) ? UsrState.Value.MasterActiveUsers : UsrState.Value.Users.Count(u => u.IsActive);
    private int InactiveUsersCount => (UsrState.Value.MasterActiveUsers > 0 || UsrState.Value.MasterInactiveUsers > 0) ? UsrState.Value.MasterInactiveUsers : UsrState.Value.Users.Count(u => !u.IsActive);
    private int AdminUsersCount => UsrState.Value.MasterAdminUsers > 0 ? UsrState.Value.MasterAdminUsers : UsrState.Value.Users.Count(u => u.UserRoles.Any(ur => ur.Role?.Name == "Admin" || (ur.Role?.Name != null && ur.Role.Name.Contains("مدیر"))));
    private int AssignedRolesUsersCount => UsrState.Value.MasterAssignedRolesUsers > 0 ? UsrState.Value.MasterAssignedRolesUsers : UsrState.Value.Users.Count(u => u.UserRoles.Any());

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // گوش دادن به رویداد موفقیت برای بستن خودکار مودال
        ActionSubscriber.SubscribeToAction<SaveUserSuccessAction>(this, action =>
        {
            isUserModalOpen = false;
            InvokeAsync(StateHasChanged);
        });

        Dispatcher.Dispatch(new LoadUserInitialDataAction());
    }

    protected override async ValueTask DisposeAsyncCore(bool disposing)
    {
        if (disposing)
        {
            ActionSubscriber.UnsubscribeFromAllActions(this);
        }
        await base.DisposeAsyncCore(disposing);
    }

    // Filters
    private void SearchTermChanged(string term) => UpdateFilterAndLoad(searchTerm: term, page: 1);
    private void PageSizeChanged(int size) => UpdateFilterAndLoad(pageSize: size, page: 1);
    private void FilterRolesChanged(List<int> v) => UpdateFilterAndLoad(roleIds: v, page: 1);
    private void FilterProjectsChanged(List<int> v) => UpdateFilterAndLoad(projectIds: v, page: 1);
    private void FilterByStatus(bool? status)
    {
        Dispatcher.Dispatch(new SetUserFilterStatusAction(status));
        Dispatcher.Dispatch(new LoadUsersAction());
    }
    private void NextPage() { if (UsrState.Value.CurrentPage < Math.Ceiling(UsrState.Value.TotalUsers / (double)UsrState.Value.PageSize)) UpdateFilterAndLoad(page: UsrState.Value.CurrentPage + 1); }
    private void PreviousPage() { if (UsrState.Value.CurrentPage > 1) UpdateFilterAndLoad(page: UsrState.Value.CurrentPage - 1); }

    private void UpdateFilterAndLoad(string? searchTerm = null, int? pageSize = null, int? page = null, List<int>? roleIds = null, List<int>? projectIds = null)
    {
        Dispatcher.Dispatch(new SetUserFiltersAction(searchTerm, pageSize, page, null, roleIds, projectIds));
        Dispatcher.Dispatch(new LoadUsersAction());
    }

    // Modal Operations
    private void OpenCreateModal()
    {
        userModel = new UserDto();
        passwordInput = string.Empty;
        selectedRoles.Clear();
        isUserModalOpen = true;
    }

    private void OpenEditModal(UserDto user)
    {
        userModel = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            IsConfirmed = user.IsConfirmed,
            IsActive = user.IsActive
        };
        passwordInput = string.Empty;
        selectedRoles = new List<string>(user.RoleNames);
        isUserModalOpen = true;
    }

    private void CloseUserModal() => isUserModalOpen = false;

    private void HandleSaveUser(dynamic payload)
    {
        UserDto user = (UserDto)payload.User;
        string password = (string)payload.Password;
        List<string> roles = (List<string>)payload.SelectedRoles;

        Dispatcher.Dispatch(new SaveUserAction(user, password, roles, UsrState.Value.AvailableRoles.ToList()));
    }

    // Delete & Bulk Operations
    private void OpenDeleteModal(UserDto user)
    {
        userToDelete = user;
        isBulkDelete = false;
        showDeleteModal = true;
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        showDeleteModal = true;
    }

    private void CloseDeleteModal() { showDeleteModal = false; userToDelete = null; isBulkDelete = false; }

    private void ConfirmDeleteUser()
    {
        showDeleteModal = false;

        if (isBulkDelete)
        {
            deletingUserIds = new HashSet<int>(selectedUserIds);
            Dispatcher.Dispatch(new ExecuteUserBulkAction(new HashSet<int>(selectedUserIds), "Delete"));
            selectedUserIds.Clear();
        }
        else if (userToDelete != null)
        {
            deletingUserIds.Add(userToDelete.Id);
            if (selectedUserIds.Contains(userToDelete.Id))
            {
                selectedUserIds.Remove(userToDelete.Id);
                selectedUserIds = new HashSet<int>(selectedUserIds);
            }
            Dispatcher.Dispatch(new ExecuteUserBulkAction(new HashSet<int>(), "SingleDelete", userToDelete.Id));
        }

        _ = Task.Delay(400).ContinueWith(_ => InvokeAsync(() => deletingUserIds.Clear()));

    }

    private void ClearSelection() => selectedUserIds.Clear();

    private void BulkDeactivateUsers()
    {
        Dispatcher.Dispatch(new ExecuteUserBulkAction(new HashSet<int>(selectedUserIds), "Deactivate"));
        selectedUserIds.Clear();
    }

    private void BulkActivateUsers()
    {
        Dispatcher.Dispatch(new ExecuteUserBulkAction(new HashSet<int>(selectedUserIds), "Activate"));
        selectedUserIds.Clear();
    }
}