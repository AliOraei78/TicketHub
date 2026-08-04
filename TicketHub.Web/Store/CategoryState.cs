using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record CategoryState(
    bool IsLoading,
    IEnumerable<CategoryDto> Categories,
    string SearchTerm,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterProjectIds,
    List<int> SelectedFilterRoleIds,
    string? StatusMessage,
    bool IsError)
{
    private CategoryState() : this(true, Array.Empty<CategoryDto>(), string.Empty, null, new(), new(), null, false) { }
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
public record SetCategoryRoleFilterAction(List<int> RoleIds);
public record SetCategoryMessageAction(string Message, bool IsError);
public record ClearCategoryMessageAction();
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

    [ReducerMethod]
    public static CategoryState ReduceSetRoleFilter(CategoryState state, SetCategoryRoleFilterAction action) =>
        state with { SelectedFilterRoleIds = action.RoleIds };

    [ReducerMethod]
    public static CategoryState ReduceSetMessage(CategoryState state, SetCategoryMessageAction action) =>
        state with { StatusMessage = action.Message, IsError = action.IsError };

    [ReducerMethod(typeof(ClearCategoryMessageAction))]
    public static CategoryState ReduceClearMessage(CategoryState state) =>
        state with { StatusMessage = null, IsError = false };
}

// 4. Effects
public class CategoryEffects
{
    private readonly ICategoryService _categoryService;
    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    private readonly ILogger<CategoryEffects> _logger;

    public CategoryEffects(
        ICategoryService categoryService,
        IProjectService projectService,
        IRoleService roleService,
        ILogger<CategoryEffects> logger)
    {
        _categoryService = categoryService;
        _projectService = projectService;
        _roleService = roleService;
        _logger = logger;
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
            dispatcher.Dispatch(new SetCategoryMessageAction("خطا در دریافت اطلاعات اولیه.", true));
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
            dispatcher.Dispatch(new SetCategoryMessageAction("خطا در دریافت لیست دسته‌بندی‌ها.", true));
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

            dispatcher.Dispatch(new SetCategoryMessageAction(action.IsEditing ? "نوع تیکت با موفقیت ویرایش شد." : "ایجاد شد.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} دسته‌بندی رخ داد.", action.IsEditing ? "ویرایش" : "ایجاد");
            dispatcher.Dispatch(new SetCategoryMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteCategory(DeleteCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن DeleteCategoryAction برای حذف دسته‌بندی {Id}.", action.Id);
            await _categoryService.DeleteAsync(new CategoryDto { Id = action.Id });
            dispatcher.Dispatch(new SetCategoryMessageAction("نوع تیکت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان حذف دسته‌بندی شناسه {Id}.", action.Id);
            dispatcher.Dispatch(new SetCategoryMessageAction("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleCategories(DeleteMultipleCategoriesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن DeleteMultipleCategoriesAction برای حذف {Count} دسته‌بندی.", action.Ids.Count());
            await _categoryService.DeleteRangeAsync(action.Ids);
            dispatcher.Dispatch(new SetCategoryMessageAction($"{action.Ids.Count()} آیتم با موفقیت حذف شدند.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان حذف گروهی دسته‌بندی‌ها.");
            dispatcher.Dispatch(new SetCategoryMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateCategoryStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن UpdateCategoryStatusAction برای تغییر وضعیت {Count} دسته‌بندی.", action.Ids.Count());
            await _categoryService.UpdateCategoriesStatusAsync(action.Ids, action.IsActive);
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            dispatcher.Dispatch(new SetCategoryMessageAction($"{action.Ids.Count()} نوع تیکت با موفقیت {actionName} شدند.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان تغییر وضعیت گروهی دسته‌بندی‌ها.");
            dispatcher.Dispatch(new SetCategoryMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}