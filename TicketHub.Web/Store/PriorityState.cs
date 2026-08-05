using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record PriorityState(
    bool IsLoading,
    IEnumerable<PriorityDto> Priorities,
    string SearchTerm,
    bool? SelectedFilterStatus)
{
    private PriorityState() : this(true, Array.Empty<PriorityDto>(), string.Empty, null) { }
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
}

// 4. Effects
public class PriorityEffects
{
    private readonly IPriorityService _service;
    private readonly ILogger<PriorityEffects> _logger;
    private readonly IToastService _toastService;

    public PriorityEffects(
        IPriorityService service,
        ILogger<PriorityEffects> logger,
        IToastService toastService)
    {
        _service = service;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadPriorities(LoadPrioritiesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست اولویت‌های سیستم.");
            var priorities = await _service.GetAllAsync();
            dispatcher.Dispatch(new PrioritiesLoadedAction(priorities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست اولویت‌ها.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات اولویت‌ها.");
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

            _toastService.ShowSuccess(action.IsEditing ? "اولویت با موفقیت ویرایش شد." : "اولویت با موفقیت ایجاد شد.");
            dispatcher.Dispatch(new LoadPrioritiesAction());
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
            _logger.LogError(ex, "خطا در زمان {ActionType} اولویت.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeletePriority(DeletePriorityAction action, IDispatcher dispatcher)
    {
        try
        {
            await _service.DeleteAsync(action.Id);
            _toastService.ShowSuccess("اولویت با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف اولویت با شناسه {PriorityId}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد! ابتدا باید تیکت‌های مرتبط با این اولویت را ویرایش کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultiplePriorities(DeleteMultiplePrioritiesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _service.DeleteRangeAsync(action.Ids);

            string verb = count > 1 ? "شدند" : "شد";
            _toastService.ShowSuccess($"{count} اولویت با موفقیت حذف {verb}.");
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی اولویت‌ها.");
            _toastService.ShowError("خطایی در حذف گروهی رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdatePriorityStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _service.UpdatePrioritiesStatusAsync(action.Ids, action.IsActive);

            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            string verb = count > 1 ? "شدند" : "شد";
            _toastService.ShowSuccess($"{count} اولویت با موفقیت {actionName} {verb}.");
            dispatcher.Dispatch(new LoadPrioritiesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی اولویت‌ها.");
            _toastService.ShowError("عملیات با خطا مواجه شد!");
        }
    }
}