using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class StatusFacade
{
    private readonly IStatusService _statusService;
    public StatusState State { get; }

    public StatusFacade(IStatusService statusService, StatusState state)
    {
        _statusService = statusService;
        State = state;
    }

    public async Task LoadStatusesAsync()
    {
        State.Statuses = await _statusService.GetAllAsync();
        State.NotifyStateChanged();
    }

    public async Task AddAsync(StatusDto dto) => await _statusService.AddAsync(dto);
    public async Task UpdateAsync(StatusDto dto) => await _statusService.UpdateAsync(dto);
    public async Task DeleteAsync(int id) => await _statusService.DeleteAsync(id);
    public async Task DeleteRangeAsync(IEnumerable<int> ids) => await _statusService.DeleteRangeAsync(ids);
}
