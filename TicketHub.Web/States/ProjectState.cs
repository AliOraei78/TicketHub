using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;

namespace TicketHub.Web.States;

public class ProjectState
{
    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;

    public ProjectState(IProjectService projectService, IRoleService roleService)
    {
        _projectService = projectService;
        _roleService = roleService;
    }

    public IEnumerable<ProjectDto> Projects { get; private set; } = new List<ProjectDto>();
    public IEnumerable<WorkflowDto> Workflows { get; private set; } = new List<WorkflowDto>();
    public IEnumerable<RoleDto> Roles { get; private set; } = new List<RoleDto>();
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
        Roles = await _roleService.GetAllRolesAsync();
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