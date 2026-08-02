using Fluxor;
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
    public StatusEffects(IStatusService statusService) => _statusService = statusService;

    [EffectMethod]
    public async Task HandleLoadStatuses(LoadStatusesAction action, IDispatcher dispatcher)
    {
        var statuses = await _statusService.GetAllAsync();
        dispatcher.Dispatch(new StatusesLoadedAction(statuses));
    }

    [EffectMethod]
    public async Task HandleSaveStatus(SaveStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            if (action.IsEditing) await _statusService.UpdateAsync(action.Status);
            else await _statusService.AddAsync(action.Status);

            dispatcher.Dispatch(new SetStatusMessageAction(action.IsEditing ? "وضعیت با موفقیت ویرایش شد." : "وضعیت با موفقیت ایجاد شد.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetStatusMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteStatus(DeleteStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _statusService.DeleteAsync(action.Id);
            dispatcher.Dispatch(new SetStatusMessageAction("وضعیت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetStatusMessageAction("امکان حذف وجود ندارد! ابتدا باید تیکت‌هایی که در این وضعیت هستند را ویرایش کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleStatuses(DeleteMultipleStatusesAction action, IDispatcher dispatcher)
    {
        try
        {
            await _statusService.DeleteRangeAsync(action.Ids);
            var verb = action.Ids.Count() == 1 ? "شد" : "شدند";
            dispatcher.Dispatch(new SetStatusMessageAction($"{action.Ids.Count()} وضعیت با موفقیت حذف {verb}.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetStatusMessageAction("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateStatusesStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _statusService.UpdateStatesStatusAsync(action.Ids, action.IsActive);
            var count = action.Ids.Count();
            var actionName = action.IsActive ? "فعال" : "غیرفعال";
            dispatcher.Dispatch(new SetStatusMessageAction($"{count} وضعیت با موفقیت {actionName} {(count == 1 ? "شد" : "شدند")}.", false));
            dispatcher.Dispatch(new LoadStatusesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetStatusMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}