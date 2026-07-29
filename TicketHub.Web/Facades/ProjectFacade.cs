using TicketHub.Application.DTOs;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class ProjectFacade
{
    private readonly IProjectService _projectService;
    public ProjectState State { get; }

    public event Action? OnChange;

    public ProjectFacade(IProjectService projectService, ProjectState state)
    {
        _projectService = projectService;
        State = state;
        State.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => OnChange?.Invoke();

    public Task InitializeAsync() => State.InitializeAsync();

    public void SetFilter(bool? status) => State.SetFilter(status);

    public void SetSearchTerm(string term) => State.SetSearchTerm(term);

    public async Task SaveProjectAsync(ProjectDto projectModel)
    {
        if (projectModel.Id == 0)
        {
            await _projectService.AddProjectAsync(projectModel);
        }
        else
        {
            await _projectService.UpdateProjectAsync(projectModel);
        }

        await State.ReloadProjectsAsync();
    }

    public async Task DeleteProjectAsync(int id)
    {
        await _projectService.DeleteProjectAsync(id);
        await State.ReloadProjectsAsync();
    }

    public void Dispose()
    {
        State.OnChange -= HandleStateChange;
    }
}