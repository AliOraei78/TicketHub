using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record TicketFieldState(
    bool IsLoading,
    IEnumerable<TicketFieldDto> TicketFields,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterCategoryIds,
    List<int> SelectedFilterFieldTypeIds,
    string? StatusMessage,
    bool IsError)
{
    private TicketFieldState() : this(true, Array.Empty<TicketFieldDto>(), string.Empty, null, new(), new(), null, false) { }
}

// 2. Actions
public record LoadTicketFieldsAction();
public record TicketFieldsLoadedAction(IEnumerable<TicketFieldDto> TicketFields);
public record SaveTicketFieldAction(TicketFieldDto TicketField, bool IsEditing);
public record DeleteTicketFieldAction(int Id);
public record DeleteMultipleTicketFieldsAction(IEnumerable<int> Ids);
public record UpdateTicketFieldStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetTicketFieldFilterStatusAction(bool? Status);
public record SetTicketFieldSearchAction(string Term);
public record SetTicketFieldCategoryFilterAction(List<int> CategoryIds);
public record SetTicketFieldTypeFilterAction(List<int> FieldTypeIds);
public record SetTicketFieldMessageAction(string Message, bool IsError);
public record ClearTicketFieldMessageAction();
public record LoadTicketFieldInitialDataAction();

// Action فرضی برای پر کردن دراپ‌داون‌ها
public record TicketFieldDependenciesLoadedAction(IEnumerable<CategoryDto> Categories, IEnumerable<FieldTypeDto> FieldTypes);

// 3. Reducers
public static class TicketFieldReducers
{
    [ReducerMethod]
    public static TicketFieldState ReduceLoadTicketFields(TicketFieldState state, LoadTicketFieldsAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static TicketFieldState ReduceTicketFieldsLoaded(TicketFieldState state, TicketFieldsLoadedAction action) =>
        state with { IsLoading = false, TicketFields = action.TicketFields };

    [ReducerMethod]
    public static TicketFieldState ReduceSetSearch(TicketFieldState state, SetTicketFieldSearchAction action) =>
        state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static TicketFieldState ReduceSetFilterStatus(TicketFieldState state, SetTicketFieldFilterStatusAction action) =>
        state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static TicketFieldState ReduceSetCategoryFilter(TicketFieldState state, SetTicketFieldCategoryFilterAction action) =>
        state with { SelectedFilterCategoryIds = action.CategoryIds };

    [ReducerMethod]
    public static TicketFieldState ReduceSetTypeFilter(TicketFieldState state, SetTicketFieldTypeFilterAction action) =>
        state with { SelectedFilterFieldTypeIds = action.FieldTypeIds };

    [ReducerMethod]
    public static TicketFieldState ReduceSetMessage(TicketFieldState state, SetTicketFieldMessageAction action) =>
        state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearTicketFieldMessageAction))]
    public static TicketFieldState ReduceClearMessage(TicketFieldState state) =>
        state with { StatusMessage = null, IsError = false };
}

// 4. Effects
public class TicketFieldEffects
{
    private readonly ITicketFieldService _ticketFieldService;
    private readonly ICategoryService _categoryService;
    private readonly IFieldTypeService _fieldTypeService;
    private readonly ILogger<TicketFieldEffects> _logger;

    public TicketFieldEffects(
        ITicketFieldService ticketFieldService,
        ICategoryService categoryService,
        IFieldTypeService fieldTypeService,
        ILogger<TicketFieldEffects> logger)
    {
        _ticketFieldService = ticketFieldService;
        _categoryService = categoryService;
        _fieldTypeService = fieldTypeService;
        _logger = logger;
    }

    [EffectMethod(typeof(LoadTicketFieldInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی اطلاعات اولیه فیلدهای تیکت (فیلدها، دسته‌بندی‌ها، انواع فیلد).");

            var fields = await _ticketFieldService.GetAllAsync();
            var categories = await _categoryService.GetAllAsync();
            var fieldTypes = await _fieldTypeService.GetAllAsync();

            dispatcher.Dispatch(new TicketFieldsLoadedAction(fields));
            dispatcher.Dispatch(new TicketFieldDependenciesLoadedAction(categories, fieldTypes));

            _logger.LogInformation("دریافت اطلاعات اولیه فیلدهای تیکت با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه فیلدهای تیکت.");
            dispatcher.Dispatch(new SetTicketFieldMessageAction("خطا در بارگذاری اطلاعات اولیه.", true));
        }
    }

    [EffectMethod]
    public async Task HandleLoadTicketFields(LoadTicketFieldsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست فیلدهای تیکت.");

            var fields = await _ticketFieldService.GetAllAsync();
            dispatcher.Dispatch(new TicketFieldsLoadedAction(fields));

            _logger.LogInformation("دریافت لیست فیلدهای تیکت با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فیلدهای تیکت.");
            dispatcher.Dispatch(new SetTicketFieldMessageAction("خطا در بارگذاری اطلاعات فیلدها.", true));
        }
    }

    [EffectMethod]
    public async Task HandleSaveTicketField(SaveTicketFieldAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveTicketFieldAction برای {ActionType} فیلد تیکت.", action.IsEditing ? "ویرایش" : "ایجاد");

            if (action.IsEditing)
                await _ticketFieldService.UpdateAsync(action.TicketField);
            else
                await _ticketFieldService.AddAsync(action.TicketField);

            dispatcher.Dispatch(new SetTicketFieldMessageAction(action.IsEditing ? "فیلد تیکت با موفقیت ویرایش شد." : "ایجاد شد.", false));
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} فیلد تیکت.", action.IsEditing ? "ویرایش" : "ایجاد");
            dispatcher.Dispatch(new SetTicketFieldMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteTicketField(DeleteTicketFieldAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف فیلد تیکت با شناسه {TicketFieldId}.", action.Id);

            await _ticketFieldService.DeleteAsync(new TicketFieldDto { Id = action.Id });

            dispatcher.Dispatch(new SetTicketFieldMessageAction("فیلد تیکت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف فیلد تیکت با شناسه {TicketFieldId}.", action.Id);
            dispatcher.Dispatch(new SetTicketFieldMessageAction("امکان حذف وجود ندارد! ارتباطات را بررسی کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleTicketFields(DeleteMultipleTicketFieldsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی فیلدهای تیکت به تعداد {Count}.", count);

            await _ticketFieldService.DeleteRangeAsync(action.Ids);

            string message = count == 1
                ? $"{count} فیلد تیکت با موفقیت حذف شد."
                : $"{count} فیلد تیکت با موفقیت حذف شدند.";

            dispatcher.Dispatch(new SetTicketFieldMessageAction(message, false));
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی فیلدهای تیکت.");
            dispatcher.Dispatch(new SetTicketFieldMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateTicketFieldStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            _logger.LogInformation("تغییر وضعیت {Count} فیلد تیکت به {Status}.", count, actionName);

            await _ticketFieldService.UpdateTicketFieldsStatusAsync(action.Ids, action.IsActive);

            string message = count == 1
                ? $"{count} فیلد تیکت با موفقیت {actionName} شد."
                : $"{count} فیلد تیکت با موفقیت {actionName} شدند.";

            dispatcher.Dispatch(new SetTicketFieldMessageAction(message, false));
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی فیلدهای تیکت.");
            dispatcher.Dispatch(new SetTicketFieldMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}