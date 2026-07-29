using TicketHub.Core.Entities;

namespace TicketHub.Application.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync();
    Task<IEnumerable<Workflow>> GetWorkflowsAsync();
    Task AddProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int id);
}