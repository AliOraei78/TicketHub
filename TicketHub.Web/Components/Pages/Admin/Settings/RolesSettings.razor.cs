using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Infrastructure.Data;
using TicketHub.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace TicketHub.Web.Components.Pages.Admin.Settings;

public partial class RolesSettings : ComponentBase
{
    [Inject]
    protected AppDbContext DbContext { get; set; } = default!;

    protected HashSet<int> SelectedRoleIds { get; set; } = new();
    protected RoleDto RoleModel { get; set; } = new();
    protected List<RoleDto>? Roles { get; set; }
    protected string? SuccessMessage { get; set; }
    protected bool IsError { get; set; }
    protected string SearchTerm { get; set; } = string.Empty;
    protected bool? SelectedFilterStatus { get; set; } = null;
    protected bool IsEditing { get; set; } = false;
    protected bool ShowDeleteModal { get; set; } = false;
    protected string DeleteModalDescription { get; set; } = string.Empty;

    private bool _isBulkDelete = false;
    private int? _editingRoleId = null;
    private RoleDto? _roleToDelete;

    protected IEnumerable<RoleDto> FilteredRoles =>
        (Roles ?? Enumerable.Empty<RoleDto>())
        .Where(r => string.IsNullOrWhiteSpace(SearchTerm) || r.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(r => SelectedFilterStatus == null || r.IsActive == SelectedFilterStatus);

    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    protected void FilterByStatus(bool? status)
    {
        SelectedFilterStatus = status;
    }

    private async Task LoadRoles()
    {
        Roles = await DbContext.Roles.ProjectToType<RoleDto>().ToListAsync();
    }

    protected async Task HandleSubmitRole()
    {
        if (string.IsNullOrWhiteSpace(RoleModel.Name)) return;

        IsError = false;

        if (IsEditing && _editingRoleId.HasValue)
        {
            var roleToUpdate = await DbContext.Roles.FindAsync(_editingRoleId.Value);
            if (roleToUpdate != null)
            {
                RoleModel.Adapt(roleToUpdate);
                SuccessMessage = "نقش با موفقیت ویرایش شد.";
            }
        }
        else
        {
            var newRole = RoleModel.Adapt<Role>();
            DbContext.Roles.Add(newRole);
            SuccessMessage = "نقش با موفقیت ایجاد شد.";
        }

        await DbContext.SaveChangesAsync();
        CancelEdit();
        await LoadRoles();

        _ = Task.Delay(3000).ContinueWith(_ => { SuccessMessage = null; InvokeAsync(StateHasChanged); });
    }

    protected void EditRole(RoleDto role)
    {
        IsEditing = true;
        _editingRoleId = role.Id;
        RoleModel = role.Adapt<RoleDto>();
    }

    protected void CancelEdit()
    {
        IsEditing = false;
        _editingRoleId = null;
        RoleModel = new RoleDto();
        SuccessMessage = null;
    }

    protected void HandleSearch(string term)
    {
        SearchTerm = term;
    }

    protected void OnSelectionChanged(HashSet<int> newKeys)
    {
        SelectedRoleIds = newKeys;
    }

    protected void ClearSelection()
    {
        SelectedRoleIds.Clear();
    }

    protected void OpenBulkDeleteModal()
    {
        _isBulkDelete = true;
        DeleteModalDescription = $"آیا از حذف {SelectedRoleIds.Count} نقش انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        ShowDeleteModal = true;
    }

    protected void OpenDeleteModal(RoleDto role)
    {
        _roleToDelete = role;
        _isBulkDelete = false;
        DeleteModalDescription = $"آیا از حذف نقش «{role.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        ShowDeleteModal = true;
    }

    protected void CancelDelete()
    {
        ShowDeleteModal = false;
        _roleToDelete = null;
        _isBulkDelete = false;
    }

    protected async Task ConfirmDelete()
    {
        try
        {
            if (_isBulkDelete)
            {
                var rolesToRemove = await DbContext.Roles.Where(r => SelectedRoleIds.Contains(r.Id)).ToListAsync();
                DbContext.Roles.RemoveRange(rolesToRemove);
                await DbContext.SaveChangesAsync();

                var verb = rolesToRemove.Count == 1 ? "شد" : "شدند";
                SuccessMessage = $"{rolesToRemove.Count} نقش با موفقیت حذف {verb}.";
                ClearSelection();
            }
            else if (_roleToDelete != null)
            {
                var roleEntity = await DbContext.Roles.FindAsync(_roleToDelete.Id);
                if (roleEntity != null)
                {
                    DbContext.Roles.Remove(roleEntity);
                    await DbContext.SaveChangesAsync();
                }

                SuccessMessage = "نقش با موفقیت حذف شد.";
                if (IsEditing && _editingRoleId == _roleToDelete.Id) CancelEdit();
            }

            IsError = false;
            await LoadRoles();
        }
        catch (Exception)
        {
            IsError = true;
            SuccessMessage = "امکان حذف وجود ندارد! ابتدا باید کاربرانی که این نقش‌ها را دارند، ویرایش کنید.";
        }
        finally
        {
            CancelDelete();
            _ = Task.Delay(4000).ContinueWith(_ => { SuccessMessage = null; IsError = false; InvokeAsync(StateHasChanged); });
        }
    }

    protected async Task BulkDeactivateRoles()
    {
        var rolesToDeactivate = await DbContext.Roles
            .Where(r => SelectedRoleIds.Contains(r.Id))
            .ToListAsync();

        foreach (var role in rolesToDeactivate)
        {
            role.IsActive = false;
        }

        await DbContext.SaveChangesAsync();

        var verb = rolesToDeactivate.Count == 1 ? "شد" : "شدند";
        SuccessMessage = $"{rolesToDeactivate.Count} نقش با موفقیت غیرفعال {verb}.";

        ClearSelection();
        await LoadRoles();

        _ = Task.Delay(4000).ContinueWith(_ => { SuccessMessage = null; InvokeAsync(StateHasChanged); });
    }

    protected async Task BulkActivateRoles()
    {
        var rolesToActivate = await DbContext.Roles
            .Where(r => SelectedRoleIds.Contains(r.Id))
            .ToListAsync();

        foreach (var role in rolesToActivate)
        {
            role.IsActive = true;
        }

        await DbContext.SaveChangesAsync();

        var verb = rolesToActivate.Count == 1 ? "شد" : "شدند";
        SuccessMessage = $"{rolesToActivate.Count} نقش با موفقیت فعال {verb}.";

        ClearSelection();
        await LoadRoles();

        _ = Task.Delay(4000).ContinueWith(_ => { SuccessMessage = null; InvokeAsync(StateHasChanged); });
    }
}