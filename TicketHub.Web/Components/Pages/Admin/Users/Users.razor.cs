using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TicketHub.Infrastructure.Data;
using TicketHub.Core.Entities;
using UserEntity = TicketHub.Core.Entities.User;

namespace TicketHub.Web.Components.Pages.Admin.Settings;

public partial class Users : ComponentBase
{
    [Inject]
    protected AppDbContext DbContext { get; set; } = default!;

    private List<UserEntity> users = new();
    private bool isLoading = true;

    private List<Role> availableRoles = new();
    private List<Project> availableProjects = new();

    private bool isUserModalOpen = false;
    private UserEntity userModel = new();
    private string passwordInput = string.Empty;
    private List<string> selectedRoles = new();
    private List<int> selectedProjectIds = new();

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

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableData();
        await LoadUsers();
    }

    private async Task LoadAvailableData()
    {
        availableRoles = await DbContext.Roles.ToListAsync();
        availableProjects = await DbContext.Projects.ToListAsync();
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        var query = DbContext.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProjects).ThenInclude(up => up.Project)
                    .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));

        totalUsers = await query.CountAsync();

        int maxPage = totalUsers == 0 ? 1 : (int)Math.Ceiling(totalUsers / (double)pageSize);
        if (currentPage > maxPage)
        {
            currentPage = maxPage;
        }

        users = await query.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();
        isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void OpenCreateModal()
    {
        userModel = new UserEntity();
        passwordInput = string.Empty;
        selectedRoles.Clear();
        selectedProjectIds.Clear();
        formErrorMessage = null;
        isUserModalOpen = true;
    }

    private async Task OpenEditModal(UserEntity user)
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
        selectedProjectIds = await DbContext.UserProjects.Where(up => up.UserId == user.Id).Select(up => up.ProjectId).ToListAsync();
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
        selectedProjectIds = payload.SelectedProjectIds;

        if (string.IsNullOrWhiteSpace(userModel.Name))
            errors.Add("• نام کاربر الزامی است.");

        if (string.IsNullOrWhiteSpace(userModel.Email))
            errors.Add("• ایمیل الزامی است.");
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userModel.Email))
            errors.Add("• فرمت ایمیل معتبر نیست.");
        else if (await DbContext.Users.AnyAsync(u => u.Email == userModel.Email && u.Id != userModel.Id))
            errors.Add("• این ایمیل قبلاً ثبت شده است.");

        if (string.IsNullOrWhiteSpace(userModel.PhoneNumber))
            errors.Add("• شماره تلفن الزامی است.");
        else
        {
            var phoneValidator = new ValidPhoneNumberAttribute();
            if (!phoneValidator.IsValid(userModel.PhoneNumber))
                errors.Add($"• {phoneValidator.ErrorMessage}");
        }

        if (userModel.Id == 0 && string.IsNullOrWhiteSpace(passwordInput))
        {
            errors.Add("• رمز عبور الزامی است.");
        }
        else if (userModel.Id == 0 || !string.IsNullOrWhiteSpace(passwordInput))
        {
            var passwordValidator = new StrongPasswordAttribute();
            if (!passwordValidator.IsValid(passwordInput))
                errors.Add($"• {passwordValidator.ErrorMessage}");
        }

        if (errors.Any())
        {
            formErrorMessage = string.Join("\n", errors);
            return;
        }

        formErrorMessage = null;

        if (userModel.Id == 0)
        {
            userModel.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);
            userModel.CreatedAt = DateTime.UtcNow;
            DbContext.Users.Add(userModel);
            await DbContext.SaveChangesAsync();
            AssignRolesAndProjects(userModel.Id);
        }
        else
        {
            var userInDb = await DbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userModel.Id);
            if (userInDb == null) return;

            userInDb.Name = userModel.Name;
            userInDb.Email = userModel.Email;
            userInDb.PhoneNumber = userModel.PhoneNumber;
            userInDb.IsConfirmed = userModel.IsConfirmed;
            userInDb.IsActive = userModel.IsActive;
            if (!string.IsNullOrWhiteSpace(passwordInput))
                userInDb.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);

            var currentRoles = await DbContext.UserRoles.Where(ur => ur.UserId == userInDb.Id).ToListAsync();
            DbContext.UserRoles.RemoveRange(currentRoles);

            var currentProjects = await DbContext.UserProjects.Where(up => up.UserId == userInDb.Id).ToListAsync();
            DbContext.UserProjects.RemoveRange(currentProjects);

            AssignRolesAndProjects(userInDb.Id);
        }

        await DbContext.SaveChangesAsync();
        CloseUserModal();
        await LoadUsers();
    }

    private void AssignRolesAndProjects(int targetUserId)
    {
        foreach (var roleName in selectedRoles.Distinct())
        {
            var role = availableRoles.FirstOrDefault(r => r.Name == roleName);
            if (role != null) DbContext.UserRoles.Add(new UserRole { UserId = targetUserId, RoleId = role.Id });
        }
        foreach (var projId in selectedProjectIds.Distinct())
        {
            DbContext.UserProjects.Add(new UserProject { UserId = targetUserId, ProjectId = projId });
        }
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

                var usersToDelete = await DbContext.Users.Where(u => selectedUserIds.Contains(u.Id)).ToListAsync();
                DbContext.Users.RemoveRange(usersToDelete);
            }
            else if (userToDelete != null)
            {
                deletingUserIds.Add(userToDelete.Id);
                StateHasChanged();
                await Task.Delay(400);

                DbContext.Users.Remove(userToDelete);
            }

            await DbContext.SaveChangesAsync();
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
        var usersToDeactivate = await DbContext.Users
            .Where(u => selectedUserIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in usersToDeactivate)
        {
            user.IsActive = false;
        }

        await DbContext.SaveChangesAsync();
        ClearSelection();
        await LoadUsers();
    }

    private async Task BulkActivateUsers()
    {
        var usersToActivate = await DbContext.Users
            .Where(u => selectedUserIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in usersToActivate)
        {
            user.IsActive = true;
        }

        await DbContext.SaveChangesAsync();
        ClearSelection();
        await LoadUsers();
    }
}