using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class StatusService : IStatusService
{
    private readonly IRepository<Status> _repository;

    public StatusService(IRepository<Status> repository)
    {
        _repository = repository;
    }

    public async Task<List<StatusDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Adapt<List<StatusDto>>();

    public async Task AddAsync(StatusDto dto) =>
        await _repository.AddAsync(dto.Adapt<Status>());

    public async Task UpdateAsync(StatusDto dto) =>
        await _repository.UpdateAsync(dto.Adapt<Status>());

    public async Task DeleteAsync(int id) =>
        await _repository.DeleteAsync(id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var entities = new List<Status>();
        foreach (var id in ids)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null) entities.Add(entity);
        }
        await _repository.DeleteRangeAsync(entities);
    }
}
