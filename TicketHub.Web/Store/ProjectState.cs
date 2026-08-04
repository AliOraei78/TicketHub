using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;

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

    public ProjectEffects(
        IProjectService projectService,
        IRoleService roleService,
        ILogger<ProjectEffects> logger)
    {
        _projectService = projectService;
        _roleService = roleService;
        _logger = logger;
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

            _logger.LogInformation("پروژه با موفقیت {ActionType} شد.", action.Project.Id == 0 ? "ایجاد" : "ویرایش");
            dispatcher.Dispatch(new LoadProjectsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} پروژه.", action.Project.Id == 0 ? "ایجاد" : "ویرایش");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteProject(DeleteProjectAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف پروژه با شناسه {ProjectId}.", action.Id);

            await _projectService.DeleteProjectAsync(action.Id);

            _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت حذف شد.", action.Id);
            dispatcher.Dispatch(new LoadProjectsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف پروژه با شناسه {ProjectId}.", action.Id);
        }
    }
}