using Fluxor;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<RoleEffects> _logger;

    public RoleEffects(
        IRoleService roleService,
        ILogger<RoleEffects> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadRoles(LoadRolesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست نقش‌ها.");

            var roles = await _roleService.GetAllRolesAsync();
            dispatcher.Dispatch(new RolesLoadedAction(roles));

            _logger.LogInformation("دریافت لیست نقش‌ها با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست نقش‌ها.");
            dispatcher.Dispatch(new SetRoleMessageAction("خطا در بارگذاری اطلاعات نقش‌ها.", true));
        }
    }

    [EffectMethod]
    public async Task HandleSaveRole(SaveRoleAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveRoleAction برای {ActionType} نقش.", action.IsEditing ? "ویرایش" : "ایجاد");

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} نقش.", action.IsEditing ? "ویرایش" : "ایجاد");
            dispatcher.Dispatch(new SetRoleMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteRole(DeleteRoleAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف نقش با شناسه {RoleId}.", action.Id);

            await _roleService.DeleteRoleAsync(action.Id);

            dispatcher.Dispatch(new SetRoleMessageAction("نقش با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف نقش با شناسه {RoleId}.", action.Id);
            dispatcher.Dispatch(new SetRoleMessageAction("امکان حذف وجود ندارد! نقش در حال استفاده است.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleRoles(DeleteMultipleRolesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی نقش‌ها به تعداد {Count}.", count);

            await _roleService.DeleteRolesAsync(action.Ids.ToHashSet());

            var verb = count == 1 ? "شد" : "شدند";
            dispatcher.Dispatch(new SetRoleMessageAction($"{count} نقش با موفقیت حذف {verb}.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی نقش‌ها.");
            dispatcher.Dispatch(new SetRoleMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateRoleStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            var statusStr = action.IsActive ? "فعال" : "غیرفعال";
            _logger.LogInformation("تغییر وضعیت {Count} نقش به {Status}.", count, statusStr);

            await _roleService.UpdateRolesStatusAsync(action.Ids.ToHashSet(), action.IsActive);

            var verb = count == 1 ? "شد" : "شدند";
            dispatcher.Dispatch(new SetRoleMessageAction($"{count} نقش با موفقیت {statusStr} {verb}.", false));
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی نقش‌ها.");
            dispatcher.Dispatch(new SetRoleMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}