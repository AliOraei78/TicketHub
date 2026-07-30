using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IWorkflowService
{
    Task<List<WorkflowDto>> GetAllAsync();
    Task<WorkflowDto?> GetByIdWithDetailsAsync(int id);
    Task<WorkflowDto> CreateAsync(WorkflowDto dto);
    Task UpdateAsync(WorkflowDto dto);
    Task DeleteAsync(int id);
    Task<List<StatusDto>> GetAllStatusesAsync();
}