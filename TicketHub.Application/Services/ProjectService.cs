using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<Workflow> _workflowRepo;

    public ProjectService(IRepository<Project> projectRepo, IRepository<Workflow> workflowRepo)
    {
        _projectRepo = projectRepo;
        _workflowRepo = workflowRepo;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync() => await _projectRepo.GetAllAsync();

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync() => await _workflowRepo.GetAllAsync();

    public async Task AddProjectAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        await _projectRepo.AddAsync(project);
    }

    public async Task UpdateProjectAsync(Project project) => await _projectRepo.UpdateAsync(project);

    public async Task DeleteProjectAsync(int id) => await _projectRepo.DeleteAsync(id);
}