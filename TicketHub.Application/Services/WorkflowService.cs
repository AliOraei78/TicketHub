using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task<List<WorkflowDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی جریان‌های کاری.");
            var workflows = await _workflowRepository.GetAllWithDetailsAsync();
            return workflows.Adapt<List<WorkflowDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست جریان‌های کاری.");
            throw;
        }
    }

    public async Task<WorkflowDto?> GetByIdWithDetailsAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی جریان کاری با شناسه {Id} به همراه جزئیات.", id);

            var workflow = await _workflowRepository.GetWorkflowWithDetailsAsync(id);
            if (workflow == null)
            {
                _logger.LogWarning("جریان کاری با شناسه {Id} یافت نشد.", id);
            }

            return workflow?.Adapt<WorkflowDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت جریان کاری با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<WorkflowDto> CreateAsync(WorkflowDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد جریان کاری جدید.");

            var entity = dto.Adapt<Workflow>();
            await _workflowRepository.AddAsync(entity);

            _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ایجاد شد.", entity.Id);
            return entity.Adapt<WorkflowDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد جریان کاری جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(WorkflowDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ویرایش جریان کاری با شناسه {Id}.", dto.Id);

            // ۱. دریافت موجودیت از دیتابیس با قابلیت Tracking
            var existingWorkflow = await _workflowRepository.GetWorkflowWithDetailsAsync(dto.Id);

            if (existingWorkflow == null)
            {
                _logger.LogWarning("جریان کاری با شناسه {Id} جهت ویرایش در دیتابیس یافت نشد. تلاش برای آپدیت مستقیم موجودیت.", dto.Id);
                var entity = dto.Adapt<Workflow>();
                await _workflowRepository.UpdateAsync(entity);
                return;
            }

            // ۲. آپدیت فیلدهای اصلی جریان کاری
            existingWorkflow.Name = dto.Name;
            existingWorkflow.Description = dto.Description;

            // ۳. مدیریت وضعیت‌ها (WorkflowStatuses) روی بوم
            var activeNodeIds = dto.WorkflowStatuses.Select(ws => ws.NodeId).ToList();
            var statusesToRemove = existingWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();

            if (statusesToRemove.Any())
            {
                _logger.LogInformation("حذف {Count} وضعیت از جریان کاری {Id}.", statusesToRemove.Count, dto.Id);
                // حذف از کالکشن والد الزامی است تا EF Core دوباره آن را اضافه نکند
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
                            // خروج از لیست پیمایشی انتقال
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

            await _workflowRepository.CommitChangesAsync();
            _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش جریان کاری با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف جریان کاری با شناسه {Id}.", id);
            await _workflowRepository.DeleteAsync(id);
            _logger.LogInformation("جریان کاری با شناسه {Id} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف جریان کاری با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<List<StatusDto>> GetAllStatusesAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست وضعیت‌های جریان کاری.");
            var statuses = await _workflowRepository.GetAllStatusesAsync();
            return statuses.Adapt<List<StatusDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست وضعیت‌های جریان کاری.");
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی جریان‌های کاری به تعداد {Count}.", idsList.Count);

            await _workflowRepository.DeleteRangeAsync(idsList);

            _logger.LogInformation("گروه جریان‌های کاری با موفقیت حذف شدند.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی جریان‌های کاری.");
            throw;
        }
    }
}