using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository<Attachment> _attachmentRepo;
    public AttachmentService(IRepository<Attachment> attachmentRepo) => _attachmentRepo = attachmentRepo;

    public async Task<List<AttachmentDto>> GetAllAsync() =>
        (await _attachmentRepo.GetAllAsync()).Adapt<List<AttachmentDto>>();

    public async Task<AttachmentDto?> GetByIdAsync(int id) =>
        (await _attachmentRepo.GetByIdAsync(id))?.Adapt<AttachmentDto>();

    public async Task<AttachmentDto> AddAsync(AttachmentDto dto)
    {
        var entity = dto.Adapt<Attachment>();
        if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;
        await _attachmentRepo.AddAsync(entity);
        return entity.Adapt<AttachmentDto>();
    }

    public async Task UpdateAsync(AttachmentDto dto) =>
        await _attachmentRepo.UpdateAsync(dto.Adapt<Attachment>());

    public async Task DeleteAsync(int id) =>
        await _attachmentRepo.DeleteAsync(id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var toDelete = (await _attachmentRepo.GetAllAsync()).Where(a => ids.Contains(a.Id)).ToList();
        if (toDelete.Any()) await _attachmentRepo.DeleteRangeAsync(toDelete);
    }
}