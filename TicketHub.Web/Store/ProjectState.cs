using Fluxor;
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

    public ProjectEffects(IProjectService projectService, IRoleService roleService)
    {
        _projectService = projectService;
        _roleService = roleService;
    }

    [EffectMethod]
    public async Task HandleLoadProjects(LoadProjectsAction action, IDispatcher dispatcher)
    {
        var workflows = await _projectService.GetWorkflowsAsync();
        var roles = await _roleService.GetAllRolesAsync();
        var projects = await _projectService.GetProjectsAsync();

        dispatcher.Dispatch(new ProjectsLoadedAction(projects, workflows, roles));
    }

    [EffectMethod]
    public async Task HandleSaveProject(SaveProjectAction action, IDispatcher dispatcher)
    {
        if (action.Project.Id == 0) await _projectService.AddProjectAsync(action.Project);
        else await _projectService.UpdateProjectAsync(action.Project);

        dispatcher.Dispatch(new LoadProjectsAction());
    }

    [EffectMethod]
    public async Task HandleDeleteProject(DeleteProjectAction action, IDispatcher dispatcher)
    {
        await _projectService.DeleteProjectAsync(action.Id);
        dispatcher.Dispatch(new LoadProjectsAction());
    }
}