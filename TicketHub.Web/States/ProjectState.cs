using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;

namespace TicketHub.Web.States;

public class ProjectState
{
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

    public void SetInitialData(IEnumerable<WorkflowDto> workflows, IEnumerable<RoleDto> roles, IEnumerable<ProjectDto> projects)
    {
        Workflows = workflows;
        Roles = roles;
        Projects = projects;
        NotifyStateChanged();
    }

    public void SetProjects(IEnumerable<ProjectDto> projects)
    {
        Projects = projects;
        NotifyStateChanged();
    }

    public void SetSearchTerm(string term) { SearchTerm = term; NotifyStateChanged(); }
    public void SetFilter(bool? status) { SelectedFilterStatus = status; NotifyStateChanged(); }
    private void NotifyStateChanged() => OnChange?.Invoke();
}