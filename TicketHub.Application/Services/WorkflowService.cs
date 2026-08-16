using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

using Microsoft.AspNetCore.Http;
using TicketHub.Application.Enums;

namespace TicketHub.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowService> _logger;
    private readonly IValidator<WorkflowDto> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowService> logger,
        IValidator<WorkflowDto> validator,
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    private async Task ValidateDtoAsync(WorkflowDto dto)
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

    public async Task<List<WorkflowDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی جریان‌های کاری.");
        var workflows = await _workflowRepository.GetAllWithDetailsAsync();
        return workflows.Adapt<List<WorkflowDto>>();
    }

    public async Task<WorkflowDto?> GetByIdWithDetailsAsync(int id)
    {
        _logger.LogInformation("جستجوی جریان کاری با شناسه {Id} به همراه جزئیات.", id);

        var workflow = await _workflowRepository.GetWorkflowWithDetailsAsync(id);

        if (workflow == null)
            throw new NotFoundException("جریان کاری", id);

        return workflow.Adapt<WorkflowDto>();
    }

    private async Task EnsurePermissionAsync(PermissionType minType, string message)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            if (!await _permissionService.HasAccessAsync(user, "/workflows", minType))
            {
                throw new ForbiddenException(message);
            }
        }
    }

    public async Task<WorkflowDto> CreateAsync(WorkflowDto dto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ایجاد جریان کاری را ندارید.");
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد جریان کاری جدید.");

        var entity = dto.Adapt<Workflow>();

        // Ensure navigation properties for transitions are correctly mapped using NodeIds
        foreach (var transition in entity.Transitions)
        {
            transition.FromStatus = entity.WorkflowStatuses.First(ws => ws.NodeId == transition.FromNodeId);
            transition.ToStatus = entity.WorkflowStatuses.First(ws => ws.NodeId == transition.ToNodeId);
        }

        await _workflowRepository.AddAsync(entity);

        _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ایجاد شد.", entity.Id);
        return entity.Adapt<WorkflowDto>();
    }

    public async Task UpdateAsync(WorkflowDto dto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ویرایش جریان کاری را ندارید.");
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ویرایش جریان کاری با شناسه {Id}.", dto.Id);

        var entity = dto.Adapt<Workflow>();
        await _workflowRepository.UpdateAsync(entity);

        _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف جریان کاری را ندارید.");
        _logger.LogWarning("درخواست حذف جریان کاری با شناسه {Id}.", id);

        var existingWorkflow = await _workflowRepository.GetWorkflowWithDetailsAsync(id);
        if (existingWorkflow == null)
            throw new NotFoundException("جریان کاری", id);

        await _workflowRepository.DeleteAsync(id);
        _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت حذف شد.", id);
    }

    public async Task<List<StatusDto>> GetAllStatusesAsync()
    {
        _logger.LogInformation("شروع دریافت لیست وضعیت‌های جریان کاری.");
        var statuses = await _workflowRepository.GetAllStatusesAsync();
        return statuses.Adapt<List<StatusDto>>();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف گروهی جریان‌های کاری را ندارید.");
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی جریان‌های کاری به تعداد {Count}.", idsList.Count);

        if (!idsList.Any())
            throw new NotFoundException("هیچ جریان کاری برای حذف انتخاب نشده است.");

        await _workflowRepository.DeleteRangeAsync(idsList);

        _logger.LogInformation("گروه جریان‌های کاری با موفقیت حذف شدند.");
    }
}