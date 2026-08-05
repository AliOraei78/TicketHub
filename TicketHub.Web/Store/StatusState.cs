using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record StatusState(
    bool IsLoading,
    IEnumerable<StatusDto> Statuses,
    string SearchTerm,
    bool? SelectedFilterStatus)
{
    private StatusState() : this(true, Array.Empty<StatusDto>(), string.Empty, null) { }
}

// 2. Actions
public record LoadStatusesAction();
public record StatusesLoadedAction(IEnumerable<StatusDto> Statuses);
public record SaveStatusAction(StatusDto Status, bool IsEditing);
public record DeleteStatusAction(int Id);
public record DeleteMultipleStatusesAction(IEnumerable<int> Ids);
public record UpdateStatusesStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetStatusFilterStatusAction(bool? Status);
public record SetStatusSearchAction(string Term);

// 3. Reducers
public static class StatusReducers
{
    [ReducerMethod]
    public static StatusState ReduceLoadStatuses(StatusState state, LoadStatusesAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static StatusState ReduceStatusesLoaded(StatusState state, StatusesLoadedAction action) =>
        state with { IsLoading = false, Statuses = action.Statuses };

    [ReducerMethod]
    public static StatusState ReduceSetSearch(StatusState state, SetStatusSearchAction action) =>
        state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static StatusState ReduceSetFilterStatus(StatusState state, SetStatusFilterStatusAction action) =>
        state with { SelectedFilterStatus = action.Status };
}

// 4. Effects
public class StatusEffects
{
    private readonly IStatusService _statusService;
    private readonly ILogger<StatusEffects> _logger;
    private readonly IToastService _toastService;

    public StatusEffects(
        IStatusService statusService,
        ILogger<StatusEffects> logger,
        IToastService toastService)
    {
        _statusService = statusService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadStatuses(LoadStatusesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست وضعیت‌ها.");
            var statuses = await _statusService.GetAllAsync();
            dispatcher.Dispatch(new StatusesLoadedAction(statuses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست وضعیت‌ها.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات وضعیت‌ها.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveStatus(SaveStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveStatusAction برای {ActionType} وضعیت.", action.IsEditing ? "ویرایش" : "ایجاد");

            if (action.IsEditing)
                await _statusService.UpdateAsync(action.Status);
            else
                await _statusService.AddAsync(action.Status);

            _toastService.ShowSuccess(action.IsEditing ? "وضعیت با موفقیت ویرایش شد." : "وضعیت با موفقیت ایجاد شد.");
            dispatcher.Dispatch(new LoadStatusesAction());
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
            _logger.LogError(ex, "خطا در زمان {ActionType} وضعیت.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteStatus(DeleteStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _statusService.DeleteAsync(action.Id);
            _toastService.ShowSuccess("وضعیت با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف وضعیت با شناسه {StatusId}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد! ابتدا باید تیکت‌هایی که در این وضعیت هستند را ویرایش کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleStatuses(DeleteMultipleStatusesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _statusService.DeleteRangeAsync(action.Ids);

            var verb = count == 1 ? "شد" : "شدند";
            _toastService.ShowSuccess($"{count} وضعیت با موفقیت حذف {verb}.");
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی وضعیت‌ها.");
            _toastService.ShowError("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateStatusesStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            var actionName = action.IsActive ? "فعال" : "غیرفعال";
            await _statusService.UpdateStatesStatusAsync(action.Ids, action.IsActive);

            _toastService.ShowSuccess($"{count} وضعیت با موفقیت {actionName} {(count == 1 ? "شد" : "شدند")}.");
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی وضعیت‌ها.");
            _toastService.ShowError("عملیات با خطا مواجه شد!");
        }
    }
}