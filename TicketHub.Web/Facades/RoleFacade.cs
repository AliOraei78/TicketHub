using System;
using System.Linq;
using System.Threading.Tasks;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Web.State;

namespace TicketHub.Web.Facades;

public class RoleFacade
{
    private readonly IRoleService _roleService;
    private readonly RoleState _state;

    public RoleFacade(IRoleService roleService, RoleState state)
    {
        _roleService = roleService;
        _state = state;
    }

    public async Task LoadRolesAsync()
    {
        _state.Roles = (await _roleService.GetAllRolesAsync()).ToList();
        _state.NotifyStateChanged();
    }

    public async Task SubmitRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(_state.RoleModel.Name)) return;
        _state.IsError = false;

        if (_state.IsEditing && _state.EditingRoleId.HasValue)
        {
            await _roleService.UpdateRoleAsync(_state.EditingRoleId.Value, _state.RoleModel);
            _state.SuccessMessage = "نقش با موفقیت ویرایش شد.";
        }
        else
        {
            await _roleService.CreateRoleAsync(_state.RoleModel);
            _state.SuccessMessage = "نقش با موفقیت ایجاد شد.";
        }

        _state.ClearForm();
        await LoadRolesAsync();
    }

    public async Task ConfirmDeleteAsync()
    {
        try
        {
            if (_state.IsBulkDelete)
            {
                await _roleService.DeleteRolesAsync(_state.SelectedRoleIds);
                var verb = _state.SelectedRoleIds.Count == 1 ? "شد" : "شدند";
                _state.SuccessMessage = $"{_state.SelectedRoleIds.Count} نقش با موفقیت حذف {verb}.";
                _state.SelectedRoleIds.Clear();
            }
            else if (_state.RoleToDelete != null)
            {
                await _roleService.DeleteRoleAsync(_state.RoleToDelete.Id);
                _state.SelectedRoleIds.Remove(_state.RoleToDelete.Id);
                _state.SuccessMessage = "نقش با موفقیت حذف شد.";
                if (_state.IsEditing && _state.EditingRoleId == _state.RoleToDelete.Id) _state.ClearForm();
            }
            _state.IsError = false;
            await LoadRolesAsync();
        }
        catch (Exception)
        {
            _state.IsError = true;
            _state.SuccessMessage = "امکان حذف وجود ندارد!";
        }
        finally
        {
            _state.ClearDeleteModal();
        }
    }

    public async Task BulkDeactivateAsync()
    {
        await _roleService.UpdateRolesStatusAsync(_state.SelectedRoleIds, false);
        var verb = _state.SelectedRoleIds.Count == 1 ? "شد" : "شدند";
        _state.SuccessMessage = $"{_state.SelectedRoleIds.Count} نقش با موفقیت غیرفعال {verb}.";
        _state.SelectedRoleIds.Clear();
        await LoadRolesAsync();
    }

    public async Task BulkActivateAsync()
    {
        await _roleService.UpdateRolesStatusAsync(_state.SelectedRoleIds, true);
        var verb = _state.SelectedRoleIds.Count == 1 ? "شد" : "شدند";
        _state.SuccessMessage = $"{_state.SelectedRoleIds.Count} نقش با موفقیت غیرفعال {verb}.";
        _state.SelectedRoleIds.Clear();
        await LoadRolesAsync();
    }
}