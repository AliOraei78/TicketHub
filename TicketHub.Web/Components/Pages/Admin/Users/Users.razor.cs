using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using UserEntity = TicketHub.Core.Entities.User;

namespace TicketHub.Web.Components.Pages.Admin.Users;

public partial class Users : ComponentBase
{
    [Inject] protected IUserRepository UserRepository { get; set; } = default!;
    [Inject] protected IRepository<Role> RoleRepository { get; set; } = default!;
    [Inject] protected IRepository<Project> ProjectRepository { get; set; } = default!;

    private List<UserEntity> users = new();
    private bool isLoading = true;

    private List<int> selectedFilterRoleIds = new();
    private List<int> selectedFilterProjectIds = new();

    private List<Role> availableRoles = new();
    private List<Project> availableProjects = new();

    private bool isUserModalOpen = false;
    private UserEntity userModel = new();
    private string passwordInput = string.Empty;
    private List<string> selectedRoles = new();

    private bool showDeleteModal = false;
    private UserEntity? userToDelete;
    private string? deleteErrorMessage;

    private string _searchTerm = string.Empty;
    private string searchTerm
    {
        get => _searchTerm;
        set { _searchTerm = value; currentPage = 1; _ = LoadUsers(); }
    }

    private int _pageSize = 10;
    private int pageSize
    {
        get => _pageSize;
        set { _pageSize = value; currentPage = 1; _ = LoadUsers(); }
    }

    private int currentPage = 1;
    private int totalUsers = 0;

    private HashSet<int> selectedUserIds = new();
    private HashSet<int> deletingUserIds = new();
    private bool isBulkDelete = false;

    private string? formErrorMessage;

    private bool? selectedFilterStatus = null;

    private async Task FilterRolesChanged(List<int> v) { selectedFilterRoleIds = v; currentPage = 1; await LoadUsers(); }
    private async Task FilterProjectsChanged(List<int> v) { selectedFilterProjectIds = v; currentPage = 1; await LoadUsers(); }
    private async Task FilterByStatus(bool? status)
    {
        selectedFilterStatus = status;
        currentPage = 1;
        await LoadUsers();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableData();
        await LoadUsers();
    }

    private async Task LoadAvailableData()
    {
        availableRoles = (await RoleRepository.GetAllAsync()).ToList();
        availableProjects = (await ProjectRepository.GetAllAsync()).ToList();
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        var result = await UserRepository.GetFilteredUsersAsync(searchTerm, selectedFilterRoleIds, selectedFilterProjectIds, selectedFilterStatus, currentPage, pageSize);
        totalUsers = result.TotalCount;
        users = result.Users;

        int maxPage = totalUsers == 0 ? 1 : (int)Math.Ceiling(totalUsers / (double)pageSize);
        if (currentPage > maxPage && maxPage > 0)
        {
            currentPage = maxPage;

            // این دو خط اضافه شدند تا در صورت برگشت به صفحه قبل، دیتای آن صفحه واکشی شود
            result = await UserRepository.GetFilteredUsersAsync(searchTerm, selectedFilterRoleIds, selectedFilterProjectIds, selectedFilterStatus, currentPage, pageSize);
            users = result.Users;
        }

        isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void OpenCreateModal()
    {
        userModel = new UserEntity();
        passwordInput = string.Empty;
        selectedRoles.Clear();
        formErrorMessage = null;
        isUserModalOpen = true;
    }

    private void OpenEditModal(UserEntity user)
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
        selectedRoles = user.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();
        formErrorMessage = null;
        isUserModalOpen = true;
    }

    private void CloseUserModal() => isUserModalOpen = false;

    private async Task HandleSaveUser(UserFormSubmissionResult payload)
    {
        var errors = new List<string>();

        userModel = payload.User;
        passwordInput = payload.Password;
        selectedRoles = payload.SelectedRoles;

        if (string.IsNullOrWhiteSpace(userModel.Name))
            errors.Add("• نام کاربر الزامی است.");

        if (string.IsNullOrWhiteSpace(userModel.Email))
            errors.Add("• ایمیل الزامی است.");
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userModel.Email))
            errors.Add("• فرمت ایمیل معتبر نیست.");
        else
        {
            // بررسی تکراری نبودن ایمیل با استفاده از متد پایه ریپازیتوری
            var allUsers = await UserRepository.GetAllAsync();
            if (allUsers.Any(u => u.Email == userModel.Email && u.Id != userModel.Id))
                errors.Add("• این ایمیل قبلاً ثبت شده است.");
        }

        if (string.IsNullOrWhiteSpace(userModel.PhoneNumber))
            errors.Add("• شماره تلفن الزامی است.");

        if (userModel.Id == 0 && string.IsNullOrWhiteSpace(passwordInput))
        {
            errors.Add("• رمز عبور الزامی است.");
        }

        if (errors.Any())
        {
            formErrorMessage = string.Join("\n", errors);
            return;
        }

        formErrorMessage = null;
        var roleIdsToAssign = availableRoles.Where(r => selectedRoles.Contains(r.Name)).Select(r => r.Id).ToList();

        if (userModel.Id == 0)
        {
            userModel.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);
            userModel.CreatedAt = DateTime.UtcNow;
            await UserRepository.AddAsync(userModel);
            await UserRepository.UpdateUserRolesAsync(userModel.Id, roleIdsToAssign);
        }
        else
        {
            var userInDb = await UserRepository.GetByIdAsync(userModel.Id);
            if (userInDb == null) return;

            userInDb.Name = userModel.Name;
            userInDb.Email = userModel.Email;
            userInDb.PhoneNumber = userModel.PhoneNumber;
            userInDb.IsConfirmed = userModel.IsConfirmed;
            userInDb.IsActive = userModel.IsActive;
            if (!string.IsNullOrWhiteSpace(passwordInput))
                userInDb.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);

            await UserRepository.UpdateAsync(userInDb);
            await UserRepository.UpdateUserRolesAsync(userInDb.Id, roleIdsToAssign);
        }

        CloseUserModal();
        await LoadUsers();
    }

    private async Task ConfirmDeleteUser()
    {
        showDeleteModal = false;

        try
        {
            if (isBulkDelete)
            {
                deletingUserIds = new HashSet<int>(selectedUserIds);
                StateHasChanged();
                await Task.Delay(400);

                await UserRepository.BulkDeleteAsync(selectedUserIds);
            }
            else if (userToDelete != null)
            {
                deletingUserIds.Add(userToDelete.Id);
                StateHasChanged();
                await Task.Delay(400);

                await UserRepository.DeleteAsync(userToDelete.Id);
            }

            selectedUserIds.Clear();
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

    private async Task NextPage()
    {
        if (currentPage < Math.Ceiling(totalUsers / (double)pageSize)) { currentPage++; await LoadUsers(); }
    }

    private async Task PreviousPage()
    {
        if (currentPage > 1) { currentPage--; await LoadUsers(); }
    }

    private void OpenDeleteModal(UserEntity user)
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

    private void ClearSelection()
    {
        selectedUserIds.Clear();
    }

    private async Task BulkDeactivateUsers()
    {
        await UserRepository.BulkUpdateStatusAsync(selectedUserIds, false);
        ClearSelection();
        await LoadUsers();
    }

    private async Task BulkActivateUsers()
    {
        await UserRepository.BulkUpdateStatusAsync(selectedUserIds, true);
        ClearSelection();
        await LoadUsers();
    }
}