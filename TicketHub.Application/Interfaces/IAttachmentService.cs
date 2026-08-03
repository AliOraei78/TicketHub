using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IAttachmentService
{
    Task<List<AttachmentDto>> GetAllAsync();
    Task<AttachmentDto?> GetByIdAsync(int id);
    Task<AttachmentDto> AddAsync(AttachmentDto dto);
    Task UpdateAsync(AttachmentDto dto);
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(IEnumerable<int> ids);
}