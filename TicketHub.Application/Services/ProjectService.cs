using Mapster;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
            IRepository<Project> projectRepo,
            IRepository<Workflow> workflowRepo,
            IRepository<RoleProject> roleProjectRepo,
            IRepository<Role> roleRepo,
            ILogger<ProjectService> logger)
    {
        _projectRepo = projectRepo;
        _workflowRepo = workflowRepo;
        _roleProjectRepo = roleProjectRepo;
        _roleRepo = roleRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی پروژه‌ها.");
            var projects = await _projectRepo.GetAllWithIncludesAsync(p => p.RoleProjects, p => p.Workflow);
            return projects.Adapt<IEnumerable<ProjectDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست پروژه‌ها.");
            throw;
        }
    }

    public async Task<IEnumerable<WorkflowDto>> GetWorkflowsAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی جریان‌های کاری.");
            var workflows = await _workflowRepo.GetAllAsync();
            return workflows.Adapt<IEnumerable<WorkflowDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست جریان‌های کاری.");
            throw;
        }
    }

    public async Task AddProjectAsync(ProjectDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد پروژه جدید.");

            var entity = dto.Adapt<Project>();
            entity.CreatedAt = DateTime.UtcNow;

            await _projectRepo.AddAsync(entity);
            _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت ایجاد شد.", entity.Id);

            if (dto.RoleIds?.Any() == true)
            {
                _logger.LogInformation("افزودن {Count} نقش به پروژه {ProjectId}.", dto.RoleIds.Count(), entity.Id);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد پروژه جدید.");
            throw;
        }
    }

    public async Task UpdateProjectAsync(ProjectDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ویرایش پروژه با شناسه {ProjectId}.", dto.Id);

            var entity = dto.Adapt<Project>();
            await _projectRepo.UpdateAsync(entity);

            var allRoleProjects = await _roleProjectRepo.GetAllAsync();
            var oldRoleProjects = allRoleProjects.Where(rp => rp.ProjectId == entity.Id).ToList();

            if (oldRoleProjects.Any())
            {
                _logger.LogInformation("حذف {Count} نقش قدیمی از پروژه {ProjectId}.", oldRoleProjects.Count, entity.Id);
                await _roleProjectRepo.DeleteRangeAsync(oldRoleProjects);
            }

            if (dto.RoleIds?.Any() == true)
            {
                _logger.LogInformation("ثبت {Count} نقش جدید برای پروژه {ProjectId}.", dto.RoleIds.Count(), entity.Id);

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

            _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت ویرایش شد.", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش پروژه با شناسه {ProjectId}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteProjectAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف پروژه با شناسه {ProjectId}.", id);
            await _projectRepo.DeleteAsync(id);
            _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف پروژه با شناسه {ProjectId}.", id);
            throw;
        }
    }

    public async Task<List<ProjectDto>> GetProjectsByUserRolesAsync(IEnumerable<string> userRoles)
    {
        try
        {
            var rolesList = userRoles.ToList();
            _logger.LogInformation("جستجوی پروژه‌ها بر اساس {Count} نقش کاربر.", rolesList.Count);

            var allRoles = await _roleRepo.GetAllAsync();
            var userRoleIds = allRoles
                .Where(r => rolesList.Contains(r.Name))
                .Select(r => r.Id)
                .ToList();

            var projects = await _projectRepo.GetAllWithIncludesAsync(p => p.RoleProjects);

            var filteredProjects = projects
                .Where(p => p.RoleProjects.Any(rp => userRoleIds.Contains(rp.RoleId)))
                .ToList();

            _logger.LogInformation("تعداد {Count} پروژه منطبق با نقش‌های کاربر یافت شد.", filteredProjects.Count);

            return filteredProjects.Adapt<List<ProjectDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در جستجوی پروژه‌ها بر اساس نقش‌های کاربر.");
            throw;
        }
    }
}