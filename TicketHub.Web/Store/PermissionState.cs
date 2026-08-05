using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

[FeatureState]
public record PermissionState(
    bool IsLoading,
    IEnumerable<PermissionDto> Permissions,
    IEnumerable<RoleDto> AvailableRoles,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterRoleIds)
{
    private PermissionState() : this(true, Array.Empty<PermissionDto>(), Array.Empty<RoleDto>(), string.Empty, null, new()) { }
}

// Actions
public record LoadPermissionsAction();
public record PermissionsLoadedAction(IEnumerable<PermissionDto> Permissions);
public record SavePermissionAction(PermissionDto Permission, bool IsEditing);
public record DeletePermissionAction(int Id);
public record DeleteMultiplePermissionsAction(IEnumerable<int> Ids);
public record UpdatePermissionStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetPermissionFilterStatusAction(bool? Status);
public record SetPermissionSearchAction(string Term);
public record SetPermissionRoleFilterAction(List<int> RoleIds);
public record AvailableRolesLoadedAction(IEnumerable<RoleDto> Roles);

// Reducers
public static class PermissionReducers
{
    [ReducerMethod]
    public static PermissionState ReduceLoadPermissions(PermissionState state, LoadPermissionsAction action) => state with { IsLoading = true };

    [ReducerMethod]
    public static PermissionState ReducePermissionsLoaded(PermissionState state, PermissionsLoadedAction action) => state with { IsLoading = false, Permissions = action.Permissions };

    [ReducerMethod]
    public static PermissionState ReduceSetSearch(PermissionState state, SetPermissionSearchAction action) => state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static PermissionState ReduceSetFilterStatus(PermissionState state, SetPermissionFilterStatusAction action) => state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static PermissionState ReduceSetRoleFilter(PermissionState state, SetPermissionRoleFilterAction action) => state with { SelectedFilterRoleIds = action.RoleIds };

    [ReducerMethod]
    public static PermissionState ReduceRolesLoaded(PermissionState state, AvailableRolesLoadedAction action) => state with { AvailableRoles = action.Roles };
}

// Effects
public class PermissionEffects
{
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;
    private readonly ILogger<PermissionEffects> _logger;
    private readonly IToastService _toastService;

    public PermissionEffects(
        IPermissionService permissionService,
        IRoleService roleService,
        ILogger<PermissionEffects> logger,
        IToastService toastService)
    {
        _permissionService = permissionService;
        _roleService = roleService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadPermissions(LoadPermissionsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست دسترسی‌ها و نقش‌های سیستم.");
            var permissions = await _permissionService.GetAllAsync();
            dispatcher.Dispatch(new PermissionsLoadedAction(permissions));

            var roles = await _roleService.GetAllRolesAsync();
            dispatcher.Dispatch(new AvailableRolesLoadedAction(roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست دسترسی‌ها یا نقش‌ها.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات دسترسی‌ها.");
        }
    }

    [EffectMethod]
    public async Task HandleSavePermission(SavePermissionAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SavePermissionAction برای {ActionType} دسترسی.", action.IsEditing ? "ویرایش" : "ایجاد");

            if (action.IsEditing)
                await _permissionService.UpdateAsync(action.Permission);
            else
                await _permissionService.CreateAsync(action.Permission);

            _toastService.ShowSuccess(action.IsEditing ? "دسترسی ویرایش شد." : "دسترسی ایجاد شد.");
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (ValidationException ex)
        {
            var errorMessage = string.Join(" | ", ex.Errors.SelectMany(e => e.Value));
            _toastService.ShowWarning(errorMessage, "خطای اطلاعات ورودی");
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} دسترسی.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطا در ذخیره اطلاعات.");
        }
    }

    [EffectMethod]
    public async Task HandleDeletePermission(DeletePermissionAction action, IDispatcher dispatcher)
    {
        try
        {
            await _permissionService.DeleteAsync(action.Id);
            _toastService.ShowSuccess("دسترسی با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف دسترسی با شناسه {PermissionId}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiple(DeleteMultiplePermissionsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _permissionService.DeleteRangeAsync(action.Ids);

            _toastService.ShowSuccess($"{count} دسترسی با موفقیت حذف شدند.");
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی دسترسی‌ها.");
            _toastService.ShowError("خطا در حذف گروهی.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePermissionStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _permissionService.UpdateStatusAsync(action.Ids, action.IsActive);

            string actionName = action.IsActive ? "فعال" : "غیر فعال";
            _toastService.ShowSuccess($"{count} دسترسی با موفقیت {actionName} شدند.");
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی دسترسی‌ها.");
            _toastService.ShowError("خطا در تغییر وضعیت.");
        }
    }
}