using System;
using System.Collections.Generic;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.State;

public class RoleState
{
    public List<RoleDto>? Roles { get; set; }
    public HashSet<int> SelectedRoleIds { get; set; } = new();
    public RoleDto RoleModel { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public bool IsError { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public bool? SelectedFilterStatus { get; set; } = null;

    public bool IsEditing { get; set; } = false;
    public int? EditingRoleId { get; set; } = null;

    public bool ShowDeleteModal { get; set; } = false;
    public string DeleteModalDescription { get; set; } = string.Empty;
    public bool IsBulkDelete { get; set; } = false;
    public RoleDto? RoleToDelete { get; set; }

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();

    public void ClearForm()
    {
        IsEditing = false;
        EditingRoleId = null;
        RoleModel = new RoleDto();
        SuccessMessage = null;
        NotifyStateChanged();
    }

    public void ClearDeleteModal()
    {
        ShowDeleteModal = false;
        RoleToDelete = null;
        IsBulkDelete = false;
        NotifyStateChanged();
    }
}