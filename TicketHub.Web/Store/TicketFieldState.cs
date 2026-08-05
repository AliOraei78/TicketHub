using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record TicketFieldState(
    bool IsLoading,
    IEnumerable<TicketFieldDto> TicketFields,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterCategoryIds,
    List<int> SelectedFilterFieldTypeIds)
{
    private TicketFieldState() : this(true, Array.Empty<TicketFieldDto>(), string.Empty, null, new(), new()) { }
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
}

// 4. Effects
public class TicketFieldEffects
{
    private readonly ITicketFieldService _ticketFieldService;
    private readonly ICategoryService _categoryService;
    private readonly IFieldTypeService _fieldTypeService;
    private readonly ILogger<TicketFieldEffects> _logger;
    private readonly IToastService _toastService;

    public TicketFieldEffects(
        ITicketFieldService ticketFieldService,
        ICategoryService categoryService,
        IFieldTypeService fieldTypeService,
        ILogger<TicketFieldEffects> logger,
        IToastService toastService)
    {
        _ticketFieldService = ticketFieldService;
        _categoryService = categoryService;
        _fieldTypeService = fieldTypeService;
        _logger = logger;
        _toastService = toastService;
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه فیلدهای تیکت.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات اولیه.");
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فیلدهای تیکت.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات فیلدها.");
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

            _toastService.ShowSuccess(action.IsEditing ? "فیلد تیکت با موفقیت ویرایش شد." : "فیلد ایجاد شد.");
            dispatcher.Dispatch(new LoadTicketFieldsAction());
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
            _logger.LogError(ex, "خطا در زمان {ActionType} فیلد تیکت.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteTicketField(DeleteTicketFieldAction action, IDispatcher dispatcher)
    {
        try
        {
            await _ticketFieldService.DeleteAsync(new TicketFieldDto { Id = action.Id });
            _toastService.ShowSuccess("فیلد تیکت با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف فیلد تیکت با شناسه {TicketFieldId}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد! ارتباطات را بررسی کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleTicketFields(DeleteMultipleTicketFieldsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            await _ticketFieldService.DeleteRangeAsync(action.Ids);

            string verb = count > 1 ? "شدند" : "شد";
            _toastService.ShowSuccess($"{count} فیلد تیکت با موفقیت حذف {verb}.");
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی فیلدهای تیکت.");
            _toastService.ShowError("خطایی در حذف گروهی رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateTicketFieldStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            string verb = count > 1 ? "شدند" : "شد";

            await _ticketFieldService.UpdateTicketFieldsStatusAsync(action.Ids, action.IsActive);

            _toastService.ShowSuccess($"{count} فیلد تیکت با موفقیت {actionName} {verb}.");
            dispatcher.Dispatch(new LoadTicketFieldsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی فیلدهای تیکت.");
            _toastService.ShowError("عملیات با خطا مواجه شد!");
        }
    }
}