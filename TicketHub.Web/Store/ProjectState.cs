using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record ProjectState(
    bool IsLoading,
    IEnumerable<ProjectDto> Projects,
    IEnumerable<WorkflowDto> Workflows,
    IEnumerable<RoleDto> Roles,
    string SearchTerm,
    bool? SelectedFilterStatus)
{
    private ProjectState() : this(true, Array.Empty<ProjectDto>(), Array.Empty<WorkflowDto>(), Array.Empty<RoleDto>(), string.Empty, null) { }
}

// 2. Actions
public record LoadProjectsAction();
public record ProjectsLoadedAction(IEnumerable<ProjectDto> Projects, IEnumerable<WorkflowDto> Workflows, IEnumerable<RoleDto> Roles);
public record SaveProjectAction(ProjectDto Project);
public record DeleteProjectAction(int Id);
public record SetProjectFilterAction(bool? Status);
public record SetProjectSearchAction(string Term);

// 3. Reducers
public static class ProjectReducers
{
    [ReducerMethod]
    public static ProjectState ReduceProjectsLoaded(ProjectState state, ProjectsLoadedAction action) =>
        state with { IsLoading = false, Projects = action.Projects, Workflows = action.Workflows, Roles = action.Roles };

    [ReducerMethod]
    public static ProjectState ReduceSetFilter(ProjectState state, SetProjectFilterAction action) =>
        state with { SelectedFilterStatus = action.Status };

    [ReducerMethod]
    public static ProjectState ReduceSetSearch(ProjectState state, SetProjectSearchAction action) =>
        state with { SearchTerm = action.Term };
}

// 4. Effects
public class ProjectEffects
{
    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    private readonly ILogger<ProjectEffects> _logger;
    private readonly IToastService _toastService;

    public ProjectEffects(
        IProjectService projectService,
        IRoleService roleService,
        ILogger<ProjectEffects> logger,
        IToastService toastService)
    {
        _projectService = projectService;
        _roleService = roleService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadProjects(LoadProjectsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست پروژه‌ها، جریان‌های کاری و نقش‌ها.");

            var workflows = await _projectService.GetWorkflowsAsync();
            var roles = await _roleService.GetAllRolesAsync();
            var projects = await _projectService.GetProjectsAsync();

            dispatcher.Dispatch(new ProjectsLoadedAction(projects, workflows, roles));

            _logger.LogInformation("دریافت اطلاعات پروژه‌ها با موفقیت انجام شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست پروژه‌ها یا اطلاعات وابسته.");
            _toastService.ShowError("خطا در دریافت اطلاعات. لطفا صفحه را مجدداً بارگذاری کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveProject(SaveProjectAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveProjectAction برای {ActionType} پروژه.", action.Project.Id == 0 ? "ایجاد" : "ویرایش");

            if (action.Project.Id == 0)
                await _projectService.AddProjectAsync(action.Project);
            else
                await _projectService.UpdateProjectAsync(action.Project);

            _toastService.ShowSuccess(action.Project.Id == 0 ? "پروژه با موفقیت ایجاد شد." : "تغییرات پروژه با موفقیت ذخیره شد.");

            dispatcher.Dispatch(new LoadProjectsAction());
        }
        catch (ValidationException ex)
        {
            // استخراج خطاهای FluentValidation و نمایش به کاربر
            var errorMessage = string.Join(" | ", ex.Errors.SelectMany(e => e.Value));
            _toastService.ShowWarning(errorMessage, "خطای اطلاعات ورودی");
        }
        catch (TicketHubException ex)
        {
            // نمایش خطاهای بیزینسی (مانند NotFoundException)
            _toastService.ShowWarning(ex.Message, "توجه");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای سیستمی غیرمنتظره در زمان {ActionType} پروژه.", action.Project.Id == 0 ? "ایجاد" : "ویرایش");
            _toastService.ShowError("یک خطای سیستمی رخ داد. لطفاً دوباره تلاش کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteProject(DeleteProjectAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف پروژه با شناسه {ProjectId}.", action.Id);

            await _projectService.DeleteProjectAsync(action.Id);

            _toastService.ShowSuccess("پروژه با موفقیت حذف شد.");
            dispatcher.Dispatch(new LoadProjectsAction());
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "تلاش برای حذف پروژه‌ای که وجود ندارد.");
            _toastService.ShowWarning(ex.Message, "پروژه یافت نشد");
            dispatcher.Dispatch(new LoadProjectsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف پروژه با شناسه {ProjectId}.", action.Id);
            _toastService.ShowError("خطا در حذف پروژه. لطفاً با پشتیبانی تماس بگیرید.");
        }
    }
}