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

public class AttachmentService : IAttachmentService
{
    private readonly IRepository<Attachment> _attachmentRepo;
    private readonly ILogger<AttachmentService> _logger;
    private readonly IValidator<AttachmentDto> _validator;

    public AttachmentService(
        IRepository<Attachment> attachmentRepo,
        ILogger<AttachmentService> logger,
        IValidator<AttachmentDto> validator)
    {
        _attachmentRepo = attachmentRepo;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(AttachmentDto dto)
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

    public async Task<List<AttachmentDto>> GetAllAsync()
    {
        _logger.LogInformation("دریافت لیست تمامی فایل‌های ضمیمه.");
        var entities = await _attachmentRepo.GetAllAsync();
        return entities.Adapt<List<AttachmentDto>>();
    }

    public async Task<AttachmentDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("دریافت فایل ضمیمه با شناسه {AttachmentId}.", id);
        var entity = await _attachmentRepo.GetByIdAsync(id);

        if (entity == null)
            throw new NotFoundException("فایل ضمیمه", id);

        return entity.Adapt<AttachmentDto>();
    }

    public async Task<AttachmentDto> AddAsync(AttachmentDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ایجاد فایل ضمیمه جدید.");

        var entity = dto.Adapt<Attachment>();
        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        await _attachmentRepo.AddAsync(entity);

        _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت ایجاد شد.", entity.Id);
        return entity.Adapt<AttachmentDto>();
    }

    public async Task UpdateAsync(AttachmentDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ویرایش فایل ضمیمه با شناسه {AttachmentId}.", dto.Id);

        var existingEntity = await _attachmentRepo.GetByIdAsync(dto.Id);
        if (existingEntity == null)
            throw new NotFoundException("فایل ضمیمه", dto.Id);

        await _attachmentRepo.UpdateAsync(dto.Adapt<Attachment>());
        _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("درخواست حذف فایل ضمیمه با شناسه {AttachmentId}.", id);

        var existingEntity = await _attachmentRepo.GetByIdAsync(id);
        if (existingEntity == null)
            throw new NotFoundException("فایل ضمیمه", id);

        await _attachmentRepo.DeleteAsync(id);
        _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت حذف شد.", id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی فایل‌های ضمیمه به تعداد {Count}.", idList.Count);

        var toDelete = (await _attachmentRepo.GetAllAsync()).Where(a => idList.Contains(a.Id)).ToList();

        if (!toDelete.Any())
            throw new NotFoundException("هیچ فایل ضمیمه‌ای برای حذف یافت نشد.");

        await _attachmentRepo.DeleteRangeAsync(toDelete);
        _logger.LogInformation("تعداد {DeletedCount} فایل ضمیمه با موفقیت حذف شدند.", toDelete.Count);
    }
}