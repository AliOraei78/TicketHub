using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record RoleState(
    bool IsLoading,
    IEnumerable<RoleDto> Roles,
    string SearchTerm,
    bool? SelectedFilterStatus,
    string? StatusMessage,
    bool IsError)
{
    private RoleState() : this(true, Array.Empty<RoleDto>(), string.Empty, null, null, false) { }
}

// 2. Actions
public record LoadRolesAction();
public record RolesLoadedAction(IEnumerable<RoleDto> Roles);
public record SaveRoleAction(RoleDto Role, bool IsEditing, int? EditingRoleId);
public record DeleteRoleAction(int Id);
public record DeleteMultipleRolesAction(IEnumerable<int> Ids);
public record UpdateRoleStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetRoleFilterStatusAction(bool? Status);
public record SetRoleSearchAction(string Term);
public record SetRoleMessageAction(string Message, bool IsError);
public record ClearRoleMessageAction();

// 3. Reducers
public static class RoleReducers
{
    [ReducerMethod]
    public static RoleState ReduceLoadRoles(RoleState state, LoadRolesAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static RoleState ReduceRolesLoaded(RoleState state, RolesLoadedAction action) =>
        state with { IsLoading = false, Roles = action.Roles };

    [ReducerMethod]
    public static RoleState ReduceSetSearch(RoleState state, SetRoleSearchAction action) =>
        state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static RoleState ReduceSetFilterStatus(RoleState state, SetRoleFilterStatusAction action) =>
        state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static RoleState ReduceSetMessage(RoleState state, SetRoleMessageAction action) =>
        state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearRoleMessageAction))]
    public static RoleState ReduceClearMessage(RoleState state) =>
        state with { StatusMessage = null, IsError = false };
}

// 4. Effects
public class RoleEffects
{
    private readonly IRoleService _roleService;
    public RoleEffects(IRoleService roleService) => _roleService = roleService;

    [EffectMethod]
    public async Task HandleLoadRoles(LoadRolesAction action, IDispatcher dispatcher)
    {
        var roles = await _roleService.GetAllRolesAsync();
        dispatcher.Dispatch(new RolesLoadedAction(roles));
    }

    [EffectMethod]
    public async Task HandleSaveRole(SaveRoleAction action, IDispatcher dispatcher)
    {
        try
        {
            if (action.IsEditing && action.EditingRoleId.HasValue)
            {
                await _roleService.UpdateRoleAsync(action.EditingRoleId.Value, action.Role);
                dispatcher.Dispatch(new SetRoleMessageAction("نقش با موفقیت ویرایش شد.", false));
            }
            else
            {
                await _roleService.CreateRoleAsync(action.Role);
                dispatcher.Dispatch(new SetRoleMessageAction("نقش با موفقیت ایجاد شد.", false));
            }

            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetRoleMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteRole(DeleteRoleAction action, IDispatcher dispatcher)
    {
        try
        {
            await _roleService.DeleteRoleAsync(action.Id);
            dispatcher.Dispatch(new SetRoleMessageAction("نقش با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetRoleMessageAction("امکان حذف وجود ندارد! نقش در حال استفاده است.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleRoles(DeleteMultipleRolesAction action, IDispatcher dispatcher)
    {
        try
        {
            await _roleService.DeleteRolesAsync(action.Ids.ToHashSet());
            var verb = action.Ids.Count() == 1 ? "شد" : "شدند";
            dispatcher.Dispatch(new SetRoleMessageAction($"{action.Ids.Count()} نقش با موفقیت حذف {verb}.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetRoleMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateRoleStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _roleService.UpdateRolesStatusAsync(action.Ids.ToHashSet(), action.IsActive);
            var verb = action.Ids.Count() == 1 ? "شد" : "شدند";
            var statusStr = action.IsActive ? "فعال" : "غیرفعال";
            dispatcher.Dispatch(new SetRoleMessageAction($"{action.Ids.Count()} نقش با موفقیت {statusStr} {verb}.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetRoleMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}