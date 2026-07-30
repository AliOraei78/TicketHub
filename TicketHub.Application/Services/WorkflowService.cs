using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;

    public WorkflowService(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<List<WorkflowDto>> GetAllAsync()
    {
        var workflows = await _workflowRepository.GetAllAsync();
        return workflows.Adapt<List<WorkflowDto>>();
    }

    public async Task<WorkflowDto?> GetByIdWithDetailsAsync(int id)
    {
        var workflow = await _workflowRepository.GetWorkflowWithDetailsAsync(id);
        return workflow?.Adapt<WorkflowDto>();
    }

    public async Task<WorkflowDto> CreateAsync(WorkflowDto dto)
    {
        var entity = dto.Adapt<Workflow>();
        await _workflowRepository.AddAsync(entity);
        return entity.Adapt<WorkflowDto>();
    }

    public async Task UpdateAsync(WorkflowDto dto)
    {
        var entity = dto.Adapt<Workflow>();
        await _workflowRepository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        await _workflowRepository.DeleteAsync(id);
    }

    public async Task<List<StatusDto>> GetAllStatusesAsync()
    {
        var statuses = await _workflowRepository.GetAllStatusesAsync();
        return statuses.Adapt<List<StatusDto>>();
    }
}