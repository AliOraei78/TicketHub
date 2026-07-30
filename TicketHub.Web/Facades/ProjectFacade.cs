using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class ProjectFacade
{
    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    public ProjectState State { get; }

    public event Action? OnChange;

    public ProjectFacade(IProjectService projectService, IRoleService roleService, ProjectState state)
    {
        _projectService = projectService;
        _roleService = roleService;
        State = state;
        State.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => OnChange?.Invoke();

    public async Task InitializeAsync()
    {
        var workflows = await _projectService.GetWorkflowsAsync();
        var roles = await _roleService.GetAllRolesAsync();
        var projects = await _projectService.GetProjectsAsync();

        State.SetInitialData(workflows, roles, projects);
    }

    private async Task ReloadProjectsAsync()
    {
        var projects = await _projectService.GetProjectsAsync();
        State.SetProjects(projects);
    }

    public async Task SaveProjectAsync(ProjectDto projectModel)
    {
        if (projectModel.Id == 0)
            await _projectService.AddProjectAsync(projectModel);
        else
            await _projectService.UpdateProjectAsync(projectModel);

        await ReloadProjectsAsync();
    }

    public async Task DeleteProjectAsync(int id)
    {
        await _projectService.DeleteProjectAsync(id);
        await ReloadProjectsAsync();
    }

    public void SetFilter(bool? status) => State.SetFilter(status);

    public void SetSearchTerm(string term) => State.SetSearchTerm(term);

    public void Dispose()
    {
        State.OnChange -= HandleStateChange;
    }
}