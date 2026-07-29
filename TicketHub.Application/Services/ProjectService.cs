using Mapster;
using TicketHub.Application.DTOs;
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

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
    {
        var projects = await _projectRepo.GetAllAsync();
        return projects.Adapt<IEnumerable<ProjectDto>>();
    }

    public async Task<IEnumerable<WorkflowDto>> GetWorkflowsAsync()
    {
        var workflows = await _workflowRepo.GetAllAsync();
        return workflows.Adapt<IEnumerable<WorkflowDto>>();
    }

    public async Task AddProjectAsync(ProjectDto dto)
    {
        var entity = dto.Adapt<Project>();
        entity.CreatedAt = DateTime.UtcNow;
        await _projectRepo.AddAsync(entity);
    }

    public async Task UpdateProjectAsync(ProjectDto dto)
    {
        var entity = dto.Adapt<Project>();
        await _projectRepo.UpdateAsync(entity);
    }

    public async Task DeleteProjectAsync(int id) => await _projectRepo.DeleteAsync(id);
}