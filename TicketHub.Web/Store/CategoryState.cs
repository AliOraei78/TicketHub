using Fluxor;
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

    // تزریق سرویس‌های مورد نیاز برای لود اولیه
    public CategoryEffects(ICategoryService categoryService, IProjectService projectService, IRoleService roleService)
    {
        _categoryService = categoryService;
        _projectService = projectService;
        _roleService = roleService;
    }

    [EffectMethod(typeof(LoadCategoryInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        // 1. واکشی اطلاعات به نوبت
        var categories = await _categoryService.GetAllAsync();
        var projects = await _projectService.GetProjectsAsync();
        var workflows = await _projectService.GetWorkflowsAsync();
        var roles = await _roleService.GetAllRolesAsync();

        // 2. ارسال دیتا به استیت کتگوری‌ها
        dispatcher.Dispatch(new CategoriesLoadedAction(categories));

        // 3. ارسال دیتای پروژه‌ها و نقش‌ها به استیت پروژه‌ها (برای دراپ‌داون‌ها)
        dispatcher.Dispatch(new ProjectsLoadedAction(projects, workflows, roles));
    }

    [EffectMethod]
    public async Task HandleLoadCategories(LoadCategoriesAction action, IDispatcher dispatcher)
    {
        var categories = await _categoryService.GetAllAsync();
        dispatcher.Dispatch(new CategoriesLoadedAction(categories));
    }

    [EffectMethod]
    public async Task HandleSaveCategory(SaveCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            if (action.IsEditing) await _categoryService.UpdateAsync(action.Category);
            else await _categoryService.AddAsync(action.Category);

            dispatcher.Dispatch(new SetCategoryMessageAction(action.IsEditing ? "نوع تیکت با موفقیت ویرایش شد." : "ایجاد شد.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetCategoryMessageAction("خطایی در ذخیره اطلاعات رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteCategory(DeleteCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            await _categoryService.DeleteAsync(new CategoryDto { Id = action.Id });
            dispatcher.Dispatch(new SetCategoryMessageAction("نوع تیکت با موفقیت حذف شد.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetCategoryMessageAction("امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.", true));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleCategories(DeleteMultipleCategoriesAction action, IDispatcher dispatcher)
    {
        try
        {
            await _categoryService.DeleteRangeAsync(action.Ids);
            dispatcher.Dispatch(new SetCategoryMessageAction($"{action.Ids.Count()} آیتم با موفقیت حذف شدند.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetCategoryMessageAction("خطایی در حذف گروهی رخ داد.", true));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateStatus(UpdateCategoryStatusAction action, IDispatcher dispatcher)
    {
        try
        {
            await _categoryService.UpdateCategoriesStatusAsync(action.Ids, action.IsActive);
            string actionName = action.IsActive ? "فعال" : "غیرفعال";
            dispatcher.Dispatch(new SetCategoryMessageAction($"{action.Ids.Count()} نوع تیکت با موفقیت {actionName} شدند.", false));
            dispatcher.Dispatch(new LoadCategoriesAction());
        }
        catch
        {
            dispatcher.Dispatch(new SetCategoryMessageAction("عملیات با خطا مواجه شد!", true));
        }
    }
}