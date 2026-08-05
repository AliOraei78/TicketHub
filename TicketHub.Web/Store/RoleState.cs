using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record RoleState(
    bool IsLoading,
    IEnumerable<RoleDto> Roles,
    string SearchTerm,
    bool? SelectedFilterStatus)
{
    private RoleState() : this(true, Array.Empty<RoleDto>(), string.Empty, null) { }
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
}

// 4. Effects
public class RoleEffects
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleEffects> _logger;
    private readonly IToastService _toastService;

    public RoleEffects(
        IRoleService roleService,
        ILogger<RoleEffects> logger,
        IToastService toastService)
    {
        _roleService = roleService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadRoles(LoadRolesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست نقش‌ها.");
            var roles = await _roleService.GetAllRolesAsync();
            dispatcher.Dispatch(new RolesLoadedAction(roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست نقش‌ها.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات نقش‌ها.");
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
                _toastService.ShowSuccess("نقش با موفقیت ویرایش شد.");
            }
            else
            {
                await _roleService.CreateRoleAsync(action.Role);
                _toastService.ShowSuccess("نقش با موفقیت ایجاد شد.");
            }

            dispatcher.Dispatch(new LoadRolesAction());
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
            _logger.LogError(ex, "خطا در زمان {ActionType} نقش.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteRole(DeleteRoleAction action, IDispatcher dispatcher)
    {
        try
        {
            await _roleService.DeleteRoleAsync(action.Id);
            _toastService.ShowSuccess("نقش با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف نقش با شناسه {RoleId}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد! نقش در حال استفاده است.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleRoles(DeleteMultipleRolesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _roleService.DeleteRolesAsync(action.Ids.ToHashSet());

            var verb = count == 1 ? "شد" : "شدند";
            _toastService.ShowSuccess($"{count} نقش با موفقیت حذف {verb}.");
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی نقش‌ها.");
            _toastService.ShowError("خطایی در حذف گروهی رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateRoleStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            var statusStr = action.IsActive ? "فعال" : "غیرفعال";
            await _roleService.UpdateRolesStatusAsync(action.Ids.ToHashSet(), action.IsActive);

            var verb = count == 1 ? "شد" : "شدند";
            _toastService.ShowSuccess($"{count} نقش با موفقیت {statusStr} {verb}.");
            dispatcher.Dispatch(new LoadRolesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی نقش‌ها.");
            _toastService.ShowError("عملیات با خطا مواجه شد!");
        }
    }
}