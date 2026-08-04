using Fluxor;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<PriorityEffects> _logger;

    public PriorityEffects(
        IPriorityService service,
        ILogger<PriorityEffects> logger)
    {
        _service = service;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadPriorities(LoadPrioritiesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست اولویت‌های سیستم.");

            var priorities = await _service.GetAllAsync();
            dispatcher.Dispatch(new PrioritiesLoadedAction(priorities));

            _logger.LogInformation("دریافت لیست اولویت‌ها با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست اولویت‌ها.");
            dispatcher.Dispatch(new SetPriorityMessageAction("خطا در بارگذاری اطلاعات اولویت‌ها.", true));
        }
    }

    [EffectMethod]
    public async Task HandleSavePriority(SavePriorityAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SavePriorityAction برای {ActionType} اولویت.", action.IsEditing ? "ویرایش" : "ایجاد");

            if (action.IsEditing)
                await _service.UpdateAsync(action.Priority);
            else
                await _service.AddAsync(action.Priority);

            dispatcher.Dispatch(new SetPriorityMessageAction(action.IsEditing ? "اولویت با موفقیت ویرایش شد." : "اولویت با موفقیت ایجاد شد.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} اولویت.", action.IsEditing ? "ویرایش" : "ایجاد");
            dispatcher.Dispatch(new SetPriorityMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeletePriority(DeletePriorityAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف اولویت با شناسه {PriorityId}.", action.Id);

            await _service.DeleteAsync(action.Id);

            dispatcher.Dispatch(new SetPriorityMessageAction("اولویت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف اولویت با شناسه {PriorityId}.", action.Id);
            dispatcher.Dispatch(new SetPriorityMessageAction("امکان حذف وجود ندارد! ابتدا باید تیکت‌های مرتبط با این اولویت را ویرایش کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiplePriorities(DeleteMultiplePrioritiesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی اولویت‌ها به تعداد {Count}.", count);

            await _service.DeleteRangeAsync(action.Ids);

            dispatcher.Dispatch(new SetPriorityMessageAction($"{count} اولویت با موفقیت حذف شدند.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی اولویت‌ها.");
            dispatcher.Dispatch(new SetPriorityMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePriorityStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            _logger.LogInformation("تغییر وضعیت {Count} اولویت به {Status}.", count, actionName);

            await _service.UpdatePrioritiesStatusAsync(action.Ids, action.IsActive);

            dispatcher.Dispatch(new SetPriorityMessageAction($"{count} اولویت با موفقیت {actionName} شدند.", false));
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی اولویت‌ها.");
            dispatcher.Dispatch(new SetPriorityMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}