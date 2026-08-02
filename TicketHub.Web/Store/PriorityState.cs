using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record PriorityState(
    bool IsLoading,
    IEnumerable<PriorityDto> Priorities,
    string SearchTerm,
    bool? SelectedFilterStatus,
    string? StatusMessage,
    bool IsError)
{
    private PriorityState() : this(true, Array.Empty<PriorityDto>(), string.Empty, null, null, false) { }
}

// 2. Actions
public record LoadPrioritiesAction();
public record PrioritiesLoadedAction(IEnumerable<PriorityDto> Priorities);
public record SavePriorityAction(PriorityDto Priority, bool IsEditing);
public record DeletePriorityAction(int Id);
public record DeleteMultiplePrioritiesAction(IEnumerable<int> Ids);
public record UpdatePriorityStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetPriorityFilterStatusAction(bool? Status);
public record SetPrioritySearchAction(string Term);
public record SetPriorityMessageAction(string Message, bool IsError);
public record ClearPriorityMessageAction();

// 3. Reducers
public static class PriorityReducers
{
    [ReducerMethod]
    public static PriorityState ReduceLoadPriorities(PriorityState state, LoadPrioritiesAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static PriorityState ReducePrioritiesLoaded(PriorityState state, PrioritiesLoadedAction action) =>
        state with { IsLoading = false, Priorities = action.Priorities };

    [ReducerMethod]
    public static PriorityState ReduceSetSearch(PriorityState state, SetPrioritySearchAction action) =>
        state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static PriorityState ReduceSetFilterStatus(PriorityState state, SetPriorityFilterStatusAction action) =>
        state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static PriorityState ReduceSetMessage(PriorityState state, SetPriorityMessageAction action) =>
        state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearPriorityMessageAction))]
    public static PriorityState ReduceClearMessage(PriorityState state) =>
        state with { StatusMessage = null, IsError = false };
}

// 4. Effects
public class PriorityEffects
{
    private readonly IPriorityService _service;
    public PriorityEffects(IPriorityService service) => _service = service;

    [EffectMethod]
    public async Task HandleLoadPriorities(LoadPrioritiesAction action, IDispatcher dispatcher)
    {
        var priorities = await _service.GetAllAsync();
        dispatcher.Dispatch(new PrioritiesLoadedAction(priorities));
    }

    [EffectMethod]
    public async Task HandleSavePriority(SavePriorityAction action, IDispatcher dispatcher)
    {
        try
        {
            if (action.IsEditing) await _service.UpdateAsync(action.Priority);
            else await _service.AddAsync(action.Priority);

            dispatcher.Dispatch(new SetPriorityMessageAction(action.IsEditing ? "اولویت با موفقیت ویرایش شد." : "اولویت با موفقیت ایجاد شد.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetPriorityMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeletePriority(DeletePriorityAction action, IDispatcher dispatcher)
    {
        try
        {
            await _service.DeleteAsync(action.Id);
            dispatcher.Dispatch(new SetPriorityMessageAction("اولویت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetPriorityMessageAction("امکان حذف وجود ندارد! ابتدا باید تیکت‌های مرتبط با این اولویت را ویرایش کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiplePriorities(DeleteMultiplePrioritiesAction action, IDispatcher dispatcher)
    {
        try
        {
            await _service.DeleteRangeAsync(action.Ids);
            dispatcher.Dispatch(new SetPriorityMessageAction($"{action.Ids.Count()} اولویت با موفقیت حذف شدند.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetPriorityMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePriorityStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _service.UpdatePrioritiesStatusAsync(action.Ids, action.IsActive);
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            dispatcher.Dispatch(new SetPriorityMessageAction($"{action.Ids.Count()} اولویت با موفقیت {actionName} شدند.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetPriorityMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}