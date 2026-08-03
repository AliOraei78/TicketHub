using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

[FeatureState]
public record PermissionState(
    bool IsLoading,
    IEnumerable<PermissionDto> Permissions,
    IEnumerable<RoleDto> AvailableRoles, // اضافه شد
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
    public static PermissionState ReduceRolesLoaded(PermissionState state, AvailableRolesLoadedAction action) =>
    state with { AvailableRoles = action.Roles };
}

// Effects
public class PermissionEffects
{
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;

    public PermissionEffects(IPermissionService permissionService, IRoleService roleService)
    {
        _permissionService = permissionService;
        _roleService = roleService;
    }

    [EffectMethod]
    public async Task HandleLoadPermissions(LoadPermissionsAction action, IDispatcher dispatcher)
    {
        // 1. واکشی دسترسی‌ها
        var permissions = await _permissionService.GetAllAsync();
        dispatcher.Dispatch(new PermissionsLoadedAction(permissions));

        // 2. واکشی نقش‌ها (بصورت متوالی برای جلوگیری از خطای DbContext)
        var roles = await _roleService.GetAllRolesAsync();
        dispatcher.Dispatch(new AvailableRolesLoadedAction(roles));
    }

    [EffectMethod]
    public async Task HandleSavePermission(SavePermissionAction action, IDispatcher dispatcher)
    {
        try
        {
            if (action.IsEditing) await _permissionService.UpdateAsync(action.Permission);
            else await _permissionService.CreateAsync(action.Permission); // تغییر AddAsync به CreateAsync

            dispatcher.Dispatch(new SetPermissionMessageAction(action.IsEditing ? "دسترسی ویرایش شد." : "ایجاد شد.", false));
            dispatcher.Dispatch(new LoadPermissionsAction()); 
        }
        catch { dispatcher.Dispatch(new SetPermissionMessageAction("خطا در ذخیره.", true)); }
    }

    [EffectMethod]
    public async Task HandleDeletePermission(DeletePermissionAction action, IDispatcher dispatcher)
    {
        try
        {
            await _permissionService.DeleteAsync(action.Id); // ارسال action.Id به جای نمونه‌سازی PermissionDto
            dispatcher.Dispatch(new SetPermissionMessageAction("حذف شد.", false)); 
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch { dispatcher.Dispatch(new SetPermissionMessageAction("امکان حذف وجود ندارد.", true)); }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiple(DeleteMultiplePermissionsAction action, IDispatcher dispatcher)
    {
        try
        {
            await _permissionService.DeleteRangeAsync(action.Ids);

            int count = action.Ids.Count();
            string message = count == 1
                ? $"{count} دسترسی با موفقیت حذف شد."
                : $"{count} دسترسی با موفقیت حذف شدند.";

            dispatcher.Dispatch(new SetPermissionMessageAction(message, false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch { dispatcher.Dispatch(new SetPermissionMessageAction("خطا در حذف گروهی.", true)); }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePermissionStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _permissionService.UpdateStatusAsync(action.Ids, action.IsActive);

            string actionName = action.IsActive ? "فعال" : "غیر فعال";
            int count = action.Ids.Count();
            string message = count == 1
                ? $"{count} دسترسی با موفقیت {actionName} شد."
                : $"{count} دسترسی با موفقیت {actionName} شدند.";

            dispatcher.Dispatch(new SetPermissionMessageAction(message, false));
            dispatcher.Dispatch(new LoadPermissionsAction());
        }
        catch { dispatcher.Dispatch(new SetPermissionMessageAction("خطا در تغییر وضعیت.", true)); }
    }
}