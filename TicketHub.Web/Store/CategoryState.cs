using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record CategoryState(
    bool IsLoading,
    IEnumerable<CategoryDto> Categories,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterProjectIds)
{
    private CategoryState() : this(true, Array.Empty<CategoryDto>(), string.Empty, null, new()) { }
}

// 2. Actions
public record LoadCategoriesAction();
public record CategoriesLoadedAction(IEnumerable<CategoryDto> Categories);
public record SaveCategoryAction(CategoryDto Category, bool IsEditing);
public record DeleteCategoryAction(int Id);
public record DeleteMultipleCategoriesAction(IEnumerable<int> Ids);
public record UpdateCategoryStatusAction(IEnumerable<int> Ids, bool IsActive);
public record SetCategoryFilterStatusAction(bool? Status);
public record SetCategorySearchAction(string Term);
public record SetCategoryProjectFilterAction(List<int> ProjectIds);
public record LoadCategoryInitialDataAction();

// 3. Reducers
public static class CategoryReducers
{
    [ReducerMethod]
    public static CategoryState ReduceLoadCategories(CategoryState state, LoadCategoriesAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static CategoryState ReduceCategoriesLoaded(CategoryState state, CategoriesLoadedAction action) =>
        state with { IsLoading = false, Categories = action.Categories };

    [ReducerMethod]
    public static CategoryState ReduceSetSearch(CategoryState state, SetCategorySearchAction action) =>
        state with { SearchTerm = action.Term };

    [ReducerMethod]
    public static CategoryState ReduceSetFilterStatus(CategoryState state, SetCategoryFilterStatusAction action) =>
        state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static CategoryState ReduceSetProjectFilter(CategoryState state, SetCategoryProjectFilterAction action) =>
        state with { SelectedFilterProjectIds = action.ProjectIds };
}

// 4. Effects
public class CategoryEffects
{
    private readonly ICategoryService _categoryService;
    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    private readonly ILogger<CategoryEffects> _logger;
    private readonly IToastService _toastService;

    public CategoryEffects(
        ICategoryService categoryService,
        IProjectService projectService,
        IRoleService roleService,
        ILogger<CategoryEffects> logger,
        IToastService toastService)
    {
        _categoryService = categoryService;
        _projectService = projectService;
        _roleService = roleService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod(typeof(LoadCategoryInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع بارگذاری دیتای اولیه دسته‌بندی‌ها، پروژه‌ها و نقش‌ها.");
            var categories = await _categoryService.GetAllAsync();
            var projects = await _projectService.GetProjectsAsync();
            var workflows = await _projectService.GetWorkflowsAsync();
            var roles = await _roleService.GetAllRolesAsync();

            dispatcher.Dispatch(new CategoriesLoadedAction(categories));
            dispatcher.Dispatch(new ProjectsLoadedAction(projects, workflows, roles));
            _logger.LogInformation("دیتای اولیه با موفقیت در استیت‌ها قرار گرفت.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بارگذاری دیتای اولیه دسته‌بندی‌ها.");
            _toastService.ShowError("خطا در دریافت اطلاعات اولیه.");
        }
    }

    [EffectMethod]
    public async Task HandleLoadCategories(LoadCategoriesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن LoadCategoriesAction برای دریافت لیست دسته‌بندی‌ها.");
            var categories = await _categoryService.GetAllAsync();
            dispatcher.Dispatch(new CategoriesLoadedAction(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست دسته‌بندی‌ها از سرویس.");
            _toastService.ShowError("خطا در دریافت لیست دسته‌بندی‌ها.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveCategory(SaveCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveCategoryAction برای {ActionType} دسته‌بندی.", action.IsEditing ? "ویرایش" : "ایجاد");
            if (action.IsEditing)
                await _categoryService.UpdateAsync(action.Category);
            else
                await _categoryService.AddAsync(action.Category);

            _toastService.ShowSuccess($"نوع تیکت با موفقیت {(action.IsEditing ? "ویرایش" : "ایجاد")} شد.");
            dispatcher.Dispatch(new LoadCategoriesAction());
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
            _logger.LogError(ex, "خطا در زمان {ActionType} دسته‌بندی رخ داد.", action.IsEditing ? "ویرایش" : "ایجاد");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteCategory(DeleteCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن DeleteCategoryAction برای حذف دسته‌بندی {Id}.", action.Id);
            await _categoryService.DeleteAsync(new CategoryDto { Id = action.Id });
            _toastService.ShowSuccess("نوع تیکت با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (NotFoundException ex)
        {
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان حذف دسته‌بندی شناسه {Id}.", action.Id);
            _toastService.ShowError("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleCategories(DeleteMultipleCategoriesAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogInformation("اجرای اکشن DeleteMultipleCategoriesAction برای حذف {Count} دسته‌بندی.", count);
            await _categoryService.DeleteRangeAsync(action.Ids);

            string verb = count > 1 ? "شدند" : "شد";
            _toastService.ShowSuccess($"{count} نوع تیکت با موفقیت حذف {verb}.");

            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان حذف گروهی دسته‌بندی‌ها.");
            _toastService.ShowError("خطایی در حذف گروهی رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateCategoryStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogInformation("اجرای اکشن UpdateCategoryStatusAction برای تغییر وضعیت {Count} دسته‌بندی.", count);
            await _categoryService.UpdateCategoriesStatusAsync(action.Ids, action.IsActive);

            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            string verb = count > 1 ? "شدند" : "شد";
            _toastService.ShowSuccess($"{count} نوع تیکت با موفقیت {actionName} {verb}.");

            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان تغییر وضعیت گروهی دسته‌بندی‌ها.");
            _toastService.ShowError("عملیات با خطا مواجه شد!");
        }
    }
}