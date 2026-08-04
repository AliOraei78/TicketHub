using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository<Attachment> _attachmentRepo;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IRepository<Attachment> attachmentRepo,
        ILogger<AttachmentService> logger)
    {
        _attachmentRepo = attachmentRepo;
        _logger = logger;
    }

    public async Task<List<AttachmentDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("دریافت لیست تمامی فایل‌های ضمیمه.");
            var entities = await _attachmentRepo.GetAllAsync();
            return entities.Adapt<List<AttachmentDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فایل‌های ضمیمه.");
            throw;
        }
    }

    public async Task<AttachmentDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("دریافت فایل ضمیمه با شناسه {AttachmentId}.", id);
            var entity = await _attachmentRepo.GetByIdAsync(id);
            return entity?.Adapt<AttachmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت فایل ضمیمه با شناسه {AttachmentId}.", id);
            throw;
        }
    }

    public async Task<AttachmentDto> AddAsync(AttachmentDto dto)
    {
        try
        {
            _logger.LogInformation("ایجاد فایل ضمیمه جدید.");
            var entity = dto.Adapt<Attachment>();
            if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;

            await _attachmentRepo.AddAsync(entity);

            _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت ایجاد شد.", entity.Id);
            return entity.Adapt<AttachmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد فایل ضمیمه جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(AttachmentDto dto)
    {
        try
        {
            _logger.LogInformation("ویرایش فایل ضمیمه با شناسه {AttachmentId}.", dto.Id);
            await _attachmentRepo.UpdateAsync(dto.Adapt<Attachment>());
            _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش فایل ضمیمه با شناسه {AttachmentId}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف فایل ضمیمه با شناسه {AttachmentId}.", id);
            await _attachmentRepo.DeleteAsync(id);
            _logger.LogInformation("فایل ضمیمه با شناسه {AttachmentId} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف فایل ضمیمه با شناسه {AttachmentId}.", id);
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            int count = ids.Count();
            _logger.LogWarning("درخواست حذف گروهی فایل‌های ضمیمه به تعداد {Count}.", count);

            var toDelete = (await _attachmentRepo.GetAllAsync()).Where(a => ids.Contains(a.Id)).ToList();
            if (toDelete.Any())
            {
                await _attachmentRepo.DeleteRangeAsync(toDelete);
                _logger.LogInformation("تعداد {DeletedCount} فایل ضمیمه با موفقیت حذف شدند.", toDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی فایل‌های ضمیمه.");
            throw;
        }
    }
}