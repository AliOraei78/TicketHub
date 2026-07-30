// Users.razor.cs
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Web.Facades;
using TicketHub.Web.States;
using UserEntity = TicketHub.Core.Entities.User;

namespace TicketHub.Web.Components.Pages.Admin.Users;

public partial class Users : ComponentBase, IDisposable
{
    [Inject] protected UserFacade UserFacade { get; set; } = default!;
    [Inject] protected UserState State { get; set; } = default!;

    private List<UserDto> users = new();
    private List<Role> availableRoles = new();
    private List<Project> availableProjects = new();

    private bool isLoading = true;
    private int totalUsers = 0;

    private bool isUserModalOpen = false;
    private UserEntity userModel = new();
    private string passwordInput = string.Empty;
    private List<string> selectedRoles = new();
    private string? formErrorMessage;

    private bool showDeleteModal = false;
    private UserDto? userToDelete;
    private string? deleteErrorMessage;
    private HashSet<int> deletingUserIds = new();
    private bool isBulkDelete = false;

    protected override async Task OnInitializedAsync()
    {
        State.OnChange += StateHasChanged;
        var data = await UserFacade.GetInitialDataAsync();
        availableRoles = data.Roles;
        availableProjects = data.Projects;
        await LoadUsers();
    }

    public void Dispose()
    {
        State.OnChange -= StateHasChanged;
    }

    private string SearchTerm
    {
        get => State.SearchTerm;
        set { State.SearchTerm = value; State.CurrentPage = 1; _ = LoadUsers(); }
    }

    private int PageSize
    {
        get => State.PageSize;
        set { State.PageSize = value; State.CurrentPage = 1; _ = LoadUsers(); }
    }

    private int CurrentPage
    {
        get => State.CurrentPage;
        set { State.CurrentPage = value; _ = LoadUsers(); }
    }

    private async Task FilterRolesChanged(List<int> v) { State.SelectedFilterRoleIds = v; State.CurrentPage = 1; await LoadUsers(); }
    private async Task FilterProjectsChanged(List<int> v) { State.SelectedFilterProjectIds = v; State.CurrentPage = 1; await LoadUsers(); }
    private async Task FilterByStatus(bool? status) { State.SelectedFilterStatus = status; State.CurrentPage = 1; await LoadUsers(); }

    private async Task LoadUsers()
    {
        isLoading = true;
        var result = await UserFacade.GetUsersAsync(State);
        users = result.Users;
        totalUsers = result.TotalCount;

        int maxPage = totalUsers == 0 ? 1 : (int)Math.Ceiling(totalUsers / (double)State.PageSize);
        if (State.CurrentPage > maxPage && maxPage > 0)
        {
            State.CurrentPage = maxPage;
            result = await UserFacade.GetUsersAsync(State);
            users = result.Users;
        }

        isLoading = false;
        State.NotifyStateChanged();
    }

    private void OpenCreateModal()
    {
        userModel = new UserEntity();
        passwordInput = string.Empty;
        selectedRoles.Clear();
        formErrorMessage = null;
        isUserModalOpen = true;
    }

    private void OpenEditModal(UserDto user)
    {
        userModel = new UserEntity
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
        selectedRoles = user.RoleNames;
        formErrorMessage = null;
        isUserModalOpen = true;
    }

    private void CloseUserModal() => isUserModalOpen = false;

    private async Task HandleSaveUser(dynamic payload)
    {
        UserEntity user = (UserEntity)payload.User;
        string password = (string)payload.Password;
        List<string> roles = (List<string>)payload.SelectedRoles;

        List<string> errors = await UserFacade.SaveUserAsync(user, password, roles, availableRoles);

        if (errors.Count > 0)
        {
            formErrorMessage = string.Join("\n", errors);
            return;
        }

        CloseUserModal();
        await LoadUsers();
    }

    private void OpenDeleteModal(UserDto user)
    {
        userToDelete = user;
        isBulkDelete = false;
        deleteErrorMessage = null;
        showDeleteModal = true;
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteErrorMessage = null;
        showDeleteModal = true;
    }

    private void CloseDeleteModal() { showDeleteModal = false; userToDelete = null; isBulkDelete = false; }

    private async Task ConfirmDeleteUser()
    {
        showDeleteModal = false;
        try
        {
            if (isBulkDelete)
            {
                deletingUserIds = new HashSet<int>(State.SelectedUserIds);
                await UserFacade.ExecuteBulkActionAsync(State.SelectedUserIds, "Delete");
            }
            else if (userToDelete != null)
            {
                deletingUserIds.Add(userToDelete.Id);
                await UserFacade.ExecuteBulkActionAsync(new HashSet<int>(), "SingleDelete", userToDelete.Id);
            }
            State.SelectedUserIds.Clear();
            deletingUserIds.Clear();
            await LoadUsers();
        }
        catch (Exception)
        {
            deleteErrorMessage = "امکان حذف کاربر(ان) وجود ندارد!";
            deletingUserIds.Clear();
            showDeleteModal = true;
            _ = Task.Delay(4000).ContinueWith(_ => { deleteErrorMessage = null; InvokeAsync(StateHasChanged); });
        }
    }

    private void ClearSelection() => State.SelectedUserIds.Clear();

    private async Task BulkDeactivateUsers()
    {
        await UserFacade.ExecuteBulkActionAsync(State.SelectedUserIds, "Deactivate");
        ClearSelection();
        await LoadUsers();
    }

    private async Task BulkActivateUsers()
    {
        await UserFacade.ExecuteBulkActionAsync(State.SelectedUserIds, "Activate");
        ClearSelection();
        await LoadUsers();
    }

    private async Task NextPage()
    {
        if (State.CurrentPage < Math.Ceiling(totalUsers / (double)State.PageSize)) { State.CurrentPage++; await LoadUsers(); }
    }

    private async Task PreviousPage()
    {
        if (State.CurrentPage > 1) { State.CurrentPage--; await LoadUsers(); }
    }
}