using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<Workflow> _workflowRepo;
    private readonly IRepository<RoleProject> _roleProjectRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly ILogger<ProjectService> _logger;
    private readonly IValidator<ProjectDto> _validator;

    public ProjectService(
            IRepository<Project> projectRepo,
            IRepository<Workflow> workflowRepo,
            IRepository<RoleProject> roleProjectRepo,
            IRepository<Role> roleRepo,
            ILogger<ProjectService> logger,
            IValidator<ProjectDto> validator)
    {
        _projectRepo = projectRepo;
        _workflowRepo = workflowRepo;
        _roleProjectRepo = roleProjectRepo;
        _roleRepo = roleRepo;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(ProjectDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(errors);
        }
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی پروژه‌ها.");
        var projects = await _projectRepo.GetAllWithIncludesAsync(p => p.RoleProjects, p => p.Workflow);
        return projects.Adapt<IEnumerable<ProjectDto>>();
    }

    public async Task<IEnumerable<WorkflowDto>> GetWorkflowsAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی جریان‌های کاری.");
        var workflows = await _workflowRepo.GetAllAsync();
        return workflows.Adapt<IEnumerable<WorkflowDto>>();
    }

    public async Task AddProjectAsync(ProjectDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد پروژه جدید.");

        var entity = dto.Adapt<Project>();
        entity.CreatedAt = DateTime.UtcNow;

        await _projectRepo.AddAsync(entity);
        _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت ایجاد شد.", entity.Id);

        if (dto.RoleIds?.Any() == true)
        {
            _logger.LogInformation("افزودن {Count} نقش به پروژه {ProjectId}.", dto.RoleIds.Count, entity.Id);

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
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ویرایش پروژه با شناسه {ProjectId}.", dto.Id);

        var existingProject = await _projectRepo.GetByIdAsync(dto.Id);
        if (existingProject == null)
            throw new NotFoundException("پروژه", dto.Id);

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
            _logger.LogInformation("ثبت {Count} نقش جدید برای پروژه {ProjectId}.", dto.RoleIds.Count, entity.Id);

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

    public async Task DeleteProjectAsync(int id)
    {
        _logger.LogWarning("درخواست حذف پروژه با شناسه {ProjectId}.", id);

        var existingProject = await _projectRepo.GetByIdAsync(id);
        if (existingProject == null)
            throw new NotFoundException("پروژه", id);

        // حذف قطعی نقش‌های متصل به پروژه برای جلوگیری از دیتای یتیم (Hard Delete)
        var allRoleProjects = await _roleProjectRepo.GetAllAsync();
        var roleProjectsToDelete = allRoleProjects.Where(rp => rp.ProjectId == id).ToList();
        if (roleProjectsToDelete.Any())
        {
            await _roleProjectRepo.DeleteRangeAsync(roleProjectsToDelete);
            _logger.LogInformation("تعداد {Count} نقش مرتبط با پروژه {Id} حذف شد.", roleProjectsToDelete.Count, id);
        }

        await _projectRepo.DeleteAsync(id); // حذف قطعی خود پروژه
        _logger.LogInformation("پروژه با شناسه {ProjectId} با موفقیت حذف شد.", id);
    }

    public async Task<List<ProjectDto>> GetProjectsByUserRolesAsync(IEnumerable<string> userRoles)
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
}