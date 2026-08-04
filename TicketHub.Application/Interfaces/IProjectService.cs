using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetProjectsAsync();
    Task<IEnumerable<WorkflowDto>> GetWorkflowsAsync();
    Task AddProjectAsync(ProjectDto project);
    Task UpdateProjectAsync(ProjectDto project);
    Task DeleteProjectAsync(int id);
    Task<List<ProjectDto>> GetProjectsByUserRolesAsync(IEnumerable<string> userRoles);
}