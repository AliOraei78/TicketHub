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

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowService> _logger;
    private readonly IValidator<WorkflowDto> _validator;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowService> logger,
        IValidator<WorkflowDto> validator)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
        _validator = validator;
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

    public async Task<WorkflowDto> CreateAsync(WorkflowDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد جریان کاری جدید.");

        var entity = dto.Adapt<Workflow>();
        await _workflowRepository.AddAsync(entity);

        _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ایجاد شد.", entity.Id);
        return entity.Adapt<WorkflowDto>();
    }

    public async Task UpdateAsync(WorkflowDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ویرایش جریان کاری با شناسه {Id}.", dto.Id);

        // ۱. دریافت موجودیت از دیتابیس با قابلیت Tracking
        var existingWorkflow = await _workflowRepository.GetWorkflowWithDetailsAsync(dto.Id);

        if (existingWorkflow == null)
            throw new NotFoundException("جریان کاری", dto.Id);

        // ۲. آپدیت فیلدهای اصلی جریان کاری
        existingWorkflow.Name = dto.Name;
        existingWorkflow.Description = dto.Description;
        existingWorkflow.IsActive = dto.IsActive;

        // ۳. مدیریت وضعیت‌ها (WorkflowStatuses) روی بوم
        var activeNodeIds = dto.WorkflowStatuses.Select(ws => ws.NodeId).ToList();
        var statusesToRemove = existingWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();

        if (statusesToRemove.Any())
        {
            _logger.LogInformation("حذف {Count} وضعیت از جریان کاری {Id}.", statusesToRemove.Count, dto.Id);
            foreach (var st in statusesToRemove) existingWorkflow.WorkflowStatuses.Remove(st);
            _workflowRepository.RemoveWorkflowStatuses(statusesToRemove);
        }

        foreach (var statusDto in dto.WorkflowStatuses)
        {
            var existingWs = existingWorkflow.WorkflowStatuses.FirstOrDefault(ws => ws.NodeId == statusDto.NodeId);
            if (existingWs != null)
            {
                existingWs.PositionX = statusDto.PositionX;
                existingWs.PositionY = statusDto.PositionY;
                existingWs.StatusId = statusDto.StatusId;
                existingWs.IsInitial = statusDto.IsInitial;
            }
            else
            {
                existingWorkflow.WorkflowStatuses.Add(statusDto.Adapt<WorkflowStatus>());
            }
        }

        // ۴. مدیریت انتقالات (Transitions)
        var activeTransitionIds = dto.Transitions.Where(t => t.Id > 0).Select(t => t.Id).ToList();

        var transitionsToRemove = existingWorkflow.Transitions
            .Where(t => !activeTransitionIds.Contains(t.Id))
            .ToList();

        if (transitionsToRemove.Any())
        {
            _logger.LogInformation("حذف {Count} انتقال از جریان کاری {Id}.", transitionsToRemove.Count, dto.Id);
            foreach (var t in transitionsToRemove) existingWorkflow.Transitions.Remove(t);
            _workflowRepository.RemoveTransitions(transitionsToRemove);
        }

        foreach (var dtoTransition in dto.Transitions)
        {
            if (dtoTransition.Id > 0)
            {
                var existingDbTransition = existingWorkflow.Transitions.FirstOrDefault(t => t.Id == dtoTransition.Id);
                if (existingDbTransition != null)
                {
                    existingDbTransition.Name = dtoTransition.Name;
                    existingDbTransition.SourcePort = dtoTransition.SourcePort;
                    existingDbTransition.TargetPort = dtoTransition.TargetPort;
                    existingDbTransition.FromNodeId = dtoTransition.FromNodeId;
                    existingDbTransition.ToNodeId = dtoTransition.ToNodeId;
                    existingDbTransition.FromState = dtoTransition.FromState;
                    existingDbTransition.ToState = dtoTransition.ToState;
                    existingDbTransition.IsAutomated = dtoTransition.IsAutomated;
                    existingDbTransition.IsActive = dtoTransition.IsActive;

                    var rolesToRemove = existingDbTransition.AllowedRoles
                        .Where(r => !dtoTransition.AllowedRoleIds.Contains(r.RoleId))
                        .ToList();

                    if (rolesToRemove.Any())
                    {
                        foreach (var role in rolesToRemove) existingDbTransition.AllowedRoles.Remove(role);
                        _workflowRepository.RemoveTransitionRoles(rolesToRemove);
                    }

                    var newRoles = dtoTransition.AllowedRoleIds
                        .Where(id => !existingDbTransition.AllowedRoles.Any(r => r.RoleId == id));
                    foreach (var id in newRoles) existingDbTransition.AllowedRoles.Add(new TransitionRole { RoleId = id });

                    var currentFieldIds = dtoTransition.TransitionFields.Where(f => f.Id > 0).Select(f => f.Id).ToList();
                    var fieldsToRemove = existingDbTransition.TransitionFields
                        .Where(f => !currentFieldIds.Contains(f.Id))
                        .ToList();

                    if (fieldsToRemove.Any())
                    {
                        foreach (var field in fieldsToRemove) existingDbTransition.TransitionFields.Remove(field);
                        _workflowRepository.RemoveTransitionFields(fieldsToRemove);
                    }

                    foreach (var fDto in dtoTransition.TransitionFields)
                    {
                        if (fDto.Id > 0)
                        {
                            var existingField = existingDbTransition.TransitionFields.FirstOrDefault(tf => tf.Id == fDto.Id);
                            if (existingField != null)
                            {
                                existingField.FieldName = fDto.FieldName;
                                existingField.FieldTypeId = fDto.FieldTypeId;
                                existingField.IsRequired = fDto.IsRequired;
                                existingField.SortOrder = fDto.SortOrder;
                                existingField.Options = fDto.Options;
                                existingField.Placeholder = fDto.Placeholder;
                                existingField.DefaultValue = fDto.DefaultValue;
                                existingField.IsActive = fDto.IsActive;
                            }
                        }
                        else
                        {
                            existingDbTransition.TransitionFields.Add(fDto.Adapt<TransitionField>());
                        }
                    }
                }
            }
            else
            {
                existingWorkflow.Transitions.Add(dtoTransition.Adapt<Transition>());
            }
        }

        await _workflowRepository.UpdateAsync(existingWorkflow);
        await _workflowRepository.CommitChangesAsync();
        _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
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
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی جریان‌های کاری به تعداد {Count}.", idsList.Count);

        if (!idsList.Any())
            throw new NotFoundException("هیچ جریان کاری برای حذف انتخاب نشده است.");

        await _workflowRepository.DeleteRangeAsync(idsList);

        _logger.LogInformation("گروه جریان‌های کاری با موفقیت حذف شدند.");
    }
}