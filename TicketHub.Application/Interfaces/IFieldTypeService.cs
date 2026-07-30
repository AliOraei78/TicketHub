using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IFieldTypeService
{
    Task<IEnumerable<FieldTypeDto>> GetAllAsync();
    Task<FieldTypeDto?> GetByIdAsync(int id);
    Task AddAsync(FieldTypeDto dto);
    Task UpdateAsync(FieldTypeDto dto);
    Task DeleteAsync(int id);
}
