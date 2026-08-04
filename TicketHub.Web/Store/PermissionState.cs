using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

[FeatureState]
public record PermissionState(
    bool IsLoading,
    IEnumerable<PermissionDto> Permissions,
    IEnumerable<RoleDto> AvailableRoles,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterRoleIds,
    string? StatusMessage,
    bool IsError)
{
    private PermissionState() : this(true, Array.Empty<PermissionDto>(), Array.Empty<RoleDto>(), string.Empty, null, new(), null, false) { }
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
public record SetPermissionMessageAction(string Message, bool IsError);
public record ClearPermissionMessageAction();
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
    public static PermissionState ReduceSetMessage(PermissionState state, SetPermissionMessageAction action) => state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearPermissionMessageAction))]
    public static PermissionState ReduceClearMessage(PermissionState state) => state with { StatusMessage = null, IsError = false };

    [ReducerMethod]
    public static PermissionState ReduceRolesLoaded(PermissionState state, AvailableRolesLoadedAction action) => state with { AvailableRoles = action.Roles };
}

// Effects
public class PermissionEffects
{
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;
    private readonly ILogger<PermissionEffects> _logger;

    public PermissionEffects(
        IPermissionService permissionService,
        IRoleService roleService,
        ILogger<PermissionEffects> logger)
    {
        _permissionService = permissionService;
        _roleService = roleService;
        _logger = logger;
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

            _logger.LogInformation("دریافت لیست دسترسی‌ها و نقش‌ها با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست دسترسی‌ها یا نقش‌ها.");
            dispatcher.Dispatch(new SetPermissionMessageAction("خطا در بارگذاری اطلاعات دسترسی‌ها.", true));
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

            dispatcher.Dispatch(new SetPermissionMessageAction(action.IsEditing ? "دسترسی ویرایش شد." : "ایجاد شد.", false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} دسترسی {PermissionTitle}.", action.IsEditing ? "ویرایش" : "ایجاد", action.Permission.Title);
            dispatcher.Dispatch(new SetPermissionMessageAction("خطا در ذخیره.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeletePermission(DeletePermissionAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف دسترسی با شناسه {PermissionId}.", action.Id);

            await _permissionService.DeleteAsync(action.Id);

            dispatcher.Dispatch(new SetPermissionMessageAction("حذف شد.", false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف دسترسی با شناسه {PermissionId}.", action.Id);
            dispatcher.Dispatch(new SetPermissionMessageAction("امکان حذف وجود ندارد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiple(DeleteMultiplePermissionsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی دسترسی‌ها به تعداد {Count}.", count);

            await _permissionService.DeleteRangeAsync(action.Ids);

            string message = count == 1
                ? $"{count} دسترسی با موفقیت حذف شد."
                : $"{count} دسترسی با موفقیت حذف شدند.";

            dispatcher.Dispatch(new SetPermissionMessageAction(message, false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی دسترسی‌ها.");
            dispatcher.Dispatch(new SetPermissionMessageAction("خطا در حذف گروهی.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePermissionStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            string actionName = action.IsActive ? "فعال" : "غیر فعال";
            _logger.LogInformation("تغییر وضعیت {Count} دسترسی به {Status}.", count, actionName);

            await _permissionService.UpdateStatusAsync(action.Ids, action.IsActive);

            string message = count == 1
                ? $"{count} دسترسی با موفقیت {actionName} شد."
                : $"{count} دسترسی با موفقیت {actionName} شدند.";

            dispatcher.Dispatch(new SetPermissionMessageAction(message, false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی دسترسی‌ها.");
            dispatcher.Dispatch(new SetPermissionMessageAction("خطا در تغییر وضعیت.", true));
        }
    }
}