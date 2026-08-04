using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record StatusState(
    bool IsLoading,
    IEnumerable<StatusDto> Statuses,
    string SearchTerm,
    bool? SelectedFilterStatus,
    string? StatusMessage,
    bool IsError)
{
    private StatusState() : this(true, Array.Empty<StatusDto>(), string.Empty, null, null, false) { }
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
public record SetStatusMessageAction(string Message, bool IsError);
public record ClearStatusMessageAction();

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

    [ReducerMethod]
    public static StatusState ReduceSetMessage(StatusState state, SetStatusMessageAction action) =>
        state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearStatusMessageAction))]
    public static StatusState ReduceClearMessage(StatusState state) =>
        state with { StatusMessage = null, IsError = false };
}

// 4. Effects
public class StatusEffects
{
    private readonly IStatusService _statusService;
    private readonly ILogger<StatusEffects> _logger;

    public StatusEffects(
        IStatusService statusService,
        ILogger<StatusEffects> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadStatuses(LoadStatusesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست وضعیت‌ها.");

            var statuses = await _statusService.GetAllAsync();
            dispatcher.Dispatch(new StatusesLoadedAction(statuses));

            _logger.LogInformation("دریافت لیست وضعیت‌ها با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست وضعیت‌ها.");
            dispatcher.Dispatch(new SetStatusMessageAction("خطا در بارگذاری اطلاعات وضعیت‌ها.", true));
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

            dispatcher.Dispatch(new SetStatusMessageAction(action.IsEditing ? "وضعیت با موفقیت ویرایش شد." : "وضعیت با موفقیت ایجاد شد.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} وضعیت.", action.IsEditing ? "ویرایش" : "ایجاد");
            dispatcher.Dispatch(new SetStatusMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteStatus(DeleteStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف وضعیت با شناسه {StatusId}.", action.Id);

            await _statusService.DeleteAsync(action.Id);

            dispatcher.Dispatch(new SetStatusMessageAction("وضعیت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف وضعیت با شناسه {StatusId}.", action.Id);
            dispatcher.Dispatch(new SetStatusMessageAction("امکان حذف وجود ندارد! ابتدا باید تیکت‌هایی که در این وضعیت هستند را ویرایش کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleStatuses(DeleteMultipleStatusesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی وضعیت‌ها به تعداد {Count}.", count);

            await _statusService.DeleteRangeAsync(action.Ids);

            var verb = count == 1 ? "شد" : "شدند";
            dispatcher.Dispatch(new SetStatusMessageAction($"{count} وضعیت با موفقیت حذف {verb}.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی وضعیت‌ها.");
            dispatcher.Dispatch(new SetStatusMessageAction("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateStatusesStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            var actionName = action.IsActive ? "فعال" : "غیرفعال";
            _logger.LogInformation("تغییر وضعیت {Count} وضعیت به {Status}.", count, actionName);

            await _statusService.UpdateStatesStatusAsync(action.Ids, action.IsActive);

            dispatcher.Dispatch(new SetStatusMessageAction($"{count} وضعیت با موفقیت {actionName} {(count == 1 ? "شد" : "شدند")}.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی وضعیت‌ها.");
            dispatcher.Dispatch(new SetStatusMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}