using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class FieldTypeService : IFieldTypeService
{
    private readonly IRepository<FieldType> _repository;

    public FieldTypeService(IRepository<FieldType> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FieldTypeDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Adapt<IEnumerable<FieldTypeDto>>();
    }

    public async Task<FieldTypeDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<FieldTypeDto>();
    }

    public async Task AddAsync(FieldTypeDto dto)
    {
        var entity = dto.Adapt<FieldType>();
        await _repository.AddAsync(entity);
    }

    public async Task UpdateAsync(FieldTypeDto dto)
    {
        var entity = dto.Adapt<FieldType>();
        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
