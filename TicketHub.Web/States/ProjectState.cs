using TicketHub.Core.Entities;
using TicketHub.Application.Services;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.States;

public class ProjectState
{
    private readonly IProjectService _projectService;

    public ProjectState(IProjectService projectService) => _projectService = projectService;

    public IEnumerable<ProjectDto> Projects { get; private set; } = new List<ProjectDto>();
    public IEnumerable<WorkflowDto> Workflows { get; private set; } = new List<WorkflowDto>();
    public string SearchTerm { get; private set; } = string.Empty;
    public bool? SelectedFilterStatus { get; private set; }

    public event Action? OnChange;

    public IEnumerable<ProjectDto> FilteredProjects =>
        Projects
        .Where(p => string.IsNullOrWhiteSpace(SearchTerm) || p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(p => SelectedFilterStatus == null || p.IsActive == SelectedFilterStatus);

    public async Task InitializeAsync()
    {
        Workflows = await _projectService.GetWorkflowsAsync();
        await ReloadProjectsAsync();
    }

    public async Task ReloadProjectsAsync()
    {
        Projects = await _projectService.GetProjectsAsync();
        NotifyStateChanged();
    }

    public void SetSearchTerm(string term) { SearchTerm = term; NotifyStateChanged(); }
    public void SetFilter(bool? status) { SelectedFilterStatus = status; NotifyStateChanged(); }
    private void NotifyStateChanged() => OnChange?.Invoke();
}