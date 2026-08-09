namespace TicketHub.Application.Services;

using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;
using System.Linq; // اضافه شد برای کوئری‌های لیست
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TicketHub.Application.Enums;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketService> _logger;
    private readonly IValidator<TicketDto> _validator;
    private readonly IProjectRepository _projectRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public TicketService(
        ITicketRepository ticketRepository,
        ILogger<TicketService> logger,
        IValidator<TicketDto> validator,
        IProjectRepository projectRepository,
        IFileStorageService fileStorageService,
        IWorkflowRepository workflowRepository,
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _validator = validator;
        _projectRepository = projectRepository;
        _fileStorageService = fileStorageService;
        _workflowRepository = workflowRepository;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    private async Task ValidateDtoAsync(TicketDto dto)
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

    public async Task<TicketDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی تیکت با شناسه {Id}.", id);

        var ticket = await _ticketRepository.GetByIdAsync(id);

        if (ticket == null)
            throw new NotFoundException("تیکت", id);

        return ticket.Adapt<TicketDto>();
    }

    public async Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId, int page, int pageSize)
    {
        _logger.LogInformation("دریافت لیست تیکت‌ها با فیلتر. صفحه: {Page}، تعداد در صفحه: {PageSize}.", page, pageSize);

        var (tickets, totalCount) = await _ticketRepository.GetFilteredTicketsAsync(
            searchTerm, projectIds, statusIds, userId, page, pageSize);

        _logger.LogInformation("تعداد {TotalCount} تیکت منطبق با فیلترها یافت شد.", totalCount);

        return (tickets.Adapt<List<TicketDto>>(), totalCount);
    }

    public async Task CreateAsync(TicketDto dto)
    {
        await ValidateDtoAsync(dto);
        _logger.LogInformation("شروع ایجاد تیکت جدید.");

        var project = await _projectRepository.GetProjectWithWorkflowAsync(dto.ProjectId);

        if (project?.Workflow?.WorkflowStatuses == null || !project.Workflow.WorkflowStatuses.Any())
            throw new ValidationException("جریان کاری معتبری برای این پروژه تعریف نشده است.");

        var initialNode = project.Workflow.WorkflowStatuses.FirstOrDefault(ws => ws.IsInitial);

        if (initialNode == null)
            throw new ValidationException("هیچ وضعیتی به عنوان وضعیت شروع (IsInitial) در جریان کاری این پروژه مشخص نشده است.");

        var ticket = dto.Adapt<Ticket>();

        ticket.WorkflowStatusId = initialNode.Id;
        ticket.StatusId = initialNode.StatusId;
        ticket.CreatedAt = DateTime.UtcNow;

        // 🌟 فیکس مشکل: انتقال دستی فیلدها به دلیل تفاوت نام در مپستر
        ticket.FieldValues = dto.FieldValues?.Select(fv => new TicketFieldValue
        {
            TicketFieldId = fv.TicketFieldId,
            Value = fv.Value ?? string.Empty
        }).ToList() ?? new List<TicketFieldValue>();

        // پردازش فایل‌های پیوست
        if (dto.FieldValues != null)
        {
            foreach (var fieldValueDto in dto.FieldValues)
            {
                if (fieldValueDto.PendingUploads != null && fieldValueDto.PendingUploads.Any())
                {
                    // حالا با خیال راحت از FirstOrDefault استفاده می‌کنیم
                    var entityFieldValue = ticket.FieldValues.FirstOrDefault(f => f.TicketFieldId == fieldValueDto.TicketFieldId);

                    if (entityFieldValue != null)
                    {
                        foreach (var upload in fieldValueDto.PendingUploads)
                        {
                            var filePath = await _fileStorageService.SaveFileAsync(upload.Content, upload.FileName);

                            entityFieldValue.Attachments.Add(new Attachment
                            {
                                FileName = upload.FileName,
                                ContentType = upload.ContentType,
                                FilePath = filePath,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }

        await _ticketRepository.AddAsync(ticket);
        _logger.LogInformation("تیکت با موفقیت ایجاد شد.");
    }

    public async Task UpdateAsync(TicketDto dto)
    {
        await ValidateDtoAsync(dto);
        _logger.LogInformation("ویرایش تیکت با شناسه {Id}.", dto.Id);

        var ticketInDb = await _ticketRepository.GetByIdAsync(dto.Id);
        if (ticketInDb == null)
            throw new NotFoundException("تیکت", dto.Id);

        if (dto.FieldValues != null)
        {
            foreach (var fieldValueDto in dto.FieldValues)
            {
                var entityFieldValue = ticketInDb.FieldValues.FirstOrDefault(f => f.TicketFieldId == fieldValueDto.TicketFieldId);

                if (entityFieldValue != null)
                {
                    // 🌟 فیکس مشکل: ویرایش مقادیر متنی و مولتی‌سلکت در زمان آپدیت تیکت
                    entityFieldValue.Value = fieldValueDto.Value ?? string.Empty;

                    // ۱. پیدا کردن و حذف پیوست‌هایی که کاربر در فرم پاک کرده است
                    var keptAttachmentIds = fieldValueDto.Attachments?.Select(a => a.Id).ToList() ?? new List<int>();
                    var attachmentsToRemove = entityFieldValue.Attachments.Where(a => !keptAttachmentIds.Contains(a.Id)).ToList();

                    foreach (var toRemove in attachmentsToRemove)
                    {
                        _fileStorageService.DeleteFile(toRemove.FilePath);
                        entityFieldValue.Attachments.Remove(toRemove);
                    }

                    // ۲. اضافه کردن فایل‌های جدید
                    if (fieldValueDto.PendingUploads != null && fieldValueDto.PendingUploads.Any())
                    {
                        foreach (var upload in fieldValueDto.PendingUploads)
                        {
                            var filePath = await _fileStorageService.SaveFileAsync(upload.Content, upload.FileName);
                            entityFieldValue.Attachments.Add(new Attachment
                            {
                                FileName = upload.FileName,
                                ContentType = upload.ContentType,
                                FilePath = filePath,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                else
                {
                    // اگر فیلد جدیدی اضافه شده بود که در دیتابیس قبلا نبوده است
                    var newFieldValue = new TicketFieldValue
                    {
                        TicketFieldId = fieldValueDto.TicketFieldId,
                        Value = fieldValueDto.Value ?? string.Empty
                    };
                    ticketInDb.FieldValues.Add(newFieldValue);
                }
            }
        }

        dto.Adapt(ticketInDb);
        await _ticketRepository.UpdateAsync(ticketInDb);

        _logger.LogInformation("تیکت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("درخواست حذف تیکت با شناسه {Id}.", id);

        var ticketInDb = await _ticketRepository.GetByIdAsync(id);
        if (ticketInDb == null)
            throw new NotFoundException("تیکت", id);

        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? user.FindFirst("sub")?.Value;
            int currentUserId = int.TryParse(userIdString, out var parsedId) ? parsedId : 0;

            bool isSender = (currentUserId > 0 && ticketInDb.UserId == currentUserId);
            bool hasFullAccess = await _permissionService.HasAccessAsync(user, "/tickets", PermissionType.Full);

            if (!isSender && !hasFullAccess)
            {
                throw new ForbiddenException("شما دسترسی لازم برای حذف این تیکت را ندارید.");
            }
        }

        if (ticketInDb.Attachments != null)
        {
            foreach (var attachment in ticketInDb.Attachments)
            {
                _fileStorageService.DeleteFile(attachment.FilePath);
            }
        }

        if (ticketInDb.FieldValues != null)
        {
            foreach (var fieldValue in ticketInDb.FieldValues)
            {
                if (fieldValue.Attachments != null)
                {
                    foreach (var attachment in fieldValue.Attachments)
                    {
                        _fileStorageService.DeleteFile(attachment.FilePath);
                    }
                }
            }
        }

        await _ticketRepository.DeleteAsync(id);

        _logger.LogInformation("تیکت با شناسه {Id} با موفقیت حذف شد.", id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی تیکت‌ها به تعداد {Count}.", idList.Count);

        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            bool hasFullAccess = await _permissionService.HasAccessAsync(user, "/tickets", PermissionType.Full);
            if (!hasFullAccess)
            {
                throw new ForbiddenException("شما دسترسی لازم برای حذف گروهی تیکت‌ها را ندارید.");
            }
        }

        foreach (var id in idList)
        {
            await DeleteAsync(id);
        }
    }

    public async Task ExecuteTransitionAsync(ExecuteTransitionDto dto, int currentUserId)
    {
        _logger.LogInformation("اجرای انتقال {TransitionId} روی تیکت {TicketId}", dto.TransitionId, dto.TicketId);

        var ticketInDb = await _ticketRepository.GetTicketWithProjectAndStatusAsync(dto.TicketId);

        if (ticketInDb == null) throw new NotFoundException("تیکت", dto.TicketId);

        var transition = await _workflowRepository.GetTransitionWithDetailsAsync(dto.TransitionId);

        if (transition == null) throw new NotFoundException("انتقال", dto.TransitionId);

        // Since transition.FromState and ToState now refer to WorkflowStatus.Id
        if (ticketInDb.WorkflowStatusId != transition.FromState)
        {
            throw new ValidationException("وضعیت فعلی تیکت با مبدا این عملیات تطابق ندارد.");
        }

        // nextWorkflowStatus is no longer needed here because we use transition.ToStatus

        var ticketHistory = new TicketHistory
        {
            TicketId = ticketInDb.Id,
            TicketTitle = ticketInDb.Title,
            TransitionId = transition.Id,
            TransitionTitle = transition.Name,
            UserId = currentUserId,
            WorkFlowId = ticketInDb.Project.WorkflowId,
            WorkFlowName = ticketInDb.Project.Workflow?.Name ?? string.Empty,
            FromStatusId = transition.FromStatus?.StatusId ?? 0,
            FromStatusName = transition.FromStatus?.Status?.Name,
            ToStatusId = transition.ToStatus?.StatusId ?? 0,
            ToStatusName = transition.ToStatus?.Status?.Name,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var field in dto.FieldValues)
        {
            var transitionField = transition.TransitionFields.FirstOrDefault(tf => tf.Id == field.TransitionFieldId);
            if (transitionField == null) continue;

            var fv = new TransitionFieldValue
            {
                TransitionFieldId = transitionField.Id,
                Value = field.Value ?? string.Empty
            };

            if (field.PendingUploads != null && field.PendingUploads.Any())
            {
                foreach (var upload in field.PendingUploads)
                {
                    var filePath = await _fileStorageService.SaveFileAsync(upload.Content, upload.FileName);
                    fv.Attachments.Add(new Attachment
                    {
                        FileName = upload.FileName,
                        ContentType = upload.ContentType,
                        FilePath = filePath,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            ticketHistory.TransitionFieldValues.Add(fv);
        }

        var nextStatusId = transition.ToStatus?.StatusId ?? 0;
        var nextWorkflowStatusId = transition.ToState;

        await _ticketRepository.ApplyTransitionAndSaveHistoryAsync(ticketInDb.Id, nextStatusId, nextWorkflowStatusId, ticketHistory);
        _logger.LogInformation("عملیات با موفقیت انجام شد.");
    }
}