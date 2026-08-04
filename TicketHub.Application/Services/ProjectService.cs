using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<Workflow> _workflowRepo;
    private readonly IRepository<RoleProject> _roleProjectRepo;
    private readonly IRepository<Role> _roleRepo;

    public ProjectService(
            IRepository<Project> projectRepo,
            IRepository<Workflow> workflowRepo,
            IRepository<RoleProject> roleProjectRepo,
            IRepository<Role> roleRepo)
    {
        _projectRepo = projectRepo;
        _workflowRepo = workflowRepo;
        _roleProjectRepo = roleProjectRepo;
        _roleRepo = roleRepo;
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
    {
        var projects = await _projectRepo.GetAllWithIncludesAsync(p => p.RoleProjects, p => p.Workflow);
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

        if (dto.RoleIds?.Any() == true)
        {
            foreach (var roleId in dto.RoleIds)
            {
                var roleProject = new RoleProject
                {
                    ProjectId = entity.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                };
                await _roleProjectRepo.AddAsync(roleProject);
            }
        }
    }

    public async Task UpdateProjectAsync(ProjectDto dto)
    {
        var entity = dto.Adapt<Project>();
        await _projectRepo.UpdateAsync(entity);

        var allRoleProjects = await _roleProjectRepo.GetAllAsync();
        var oldRoleProjects = allRoleProjects.Where(rp => rp.ProjectId == entity.Id).ToList();

        if (oldRoleProjects.Any())
        {
            await _roleProjectRepo.DeleteRangeAsync(oldRoleProjects);
        }

        if (dto.RoleIds?.Any() == true)
        {
            foreach (var roleId in dto.RoleIds)
            {
                var roleProject = new RoleProject
                {
                    ProjectId = entity.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                };
                await _roleProjectRepo.AddAsync(roleProject);
            }
        }
    }

    public async Task DeleteProjectAsync(int id) => await _projectRepo.DeleteAsync(id);

    public async Task<List<ProjectDto>> GetProjectsByUserRolesAsync(IEnumerable<string> userRoles)
    {
        var allRoles = await _roleRepo.GetAllAsync();
        var userRoleIds = allRoles
            .Where(r => userRoles.Contains(r.Name))
            .Select(r => r.Id)
            .ToList();

        var projects = await _projectRepo.GetAllWithIncludesAsync(p => p.RoleProjects);

        var filteredProjects = projects
            .Where(p => p.RoleProjects.Any(rp => userRoleIds.Contains(rp.RoleId)))
            .ToList();

        return filteredProjects.Adapt<List<ProjectDto>>();
    }
}