// TicketHub.Infrastructure/Repositories/WorkflowRepository.cs
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TicketHub.Core.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories;

public class WorkflowRepository : GenericRepository<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<Workflow?> GetWorkflowWithDetailsAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Workflow>()
            .Include(w => w.WorkflowStatuses)
                .ThenInclude(ws => ws.Status)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.AllowedRoles)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.TransitionFields)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Set<Workflow>()
            .Include(w => w.Transitions)
            .Include(w => w.WorkflowStatuses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);

        if (entity != null)
        {
            if (entity.Transitions.Any())
            {
                context.Set<Transition>().RemoveRange(entity.Transitions);
            }

            if (entity.WorkflowStatuses.Any())
            {
                context.Set<WorkflowStatus>().RemoveRange(entity.WorkflowStatuses);
            }

            context.Set<Workflow>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Status>> GetAllStatusesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Status>().ToListAsync();
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Project>().ToListAsync();
    }

    public async Task<List<WorkflowStatus>> GetStatusesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<WorkflowStatus>().ToListAsync();
    }

    public void RemoveTransitionRoles(IEnumerable<TransitionRole> roles)
    {
        using var context = _factory.CreateDbContext();
        context.Set<TransitionRole>().RemoveRange(roles);
        context.SaveChanges();
    }

    public void RemoveTransitionFields(IEnumerable<TransitionField> fields)
    {
        using var context = _factory.CreateDbContext();
        context.Set<TransitionField>().RemoveRange(fields);
        context.SaveChanges();
    }

    public void RemoveWorkflowStatuses(IEnumerable<WorkflowStatus> statuses)
    {
        using var context = _factory.CreateDbContext();
        context.Set<WorkflowStatus>().RemoveRange(statuses);
        context.SaveChanges();
    }

    public void RemoveTransitions(IEnumerable<Transition> transitions)
    {
        using var context = _factory.CreateDbContext();
        context.Set<Transition>().RemoveRange(transitions);
        context.SaveChanges();
    }

    public async Task CommitChangesAsync()
    {
        // در الگوی Factory تغییرات درون خود متدها (به صورت ایزوله) Save می‌شوند
        await Task.CompletedTask;
    }

    public async Task<List<Workflow>> GetAllWithDetailsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Workflow>()
            .Include(w => w.WorkflowStatuses)
            .Include(w => w.Transitions)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        using var context = await _factory.CreateDbContextAsync();
        await context.Set<Workflow>()
            .Where(w => ids.Contains(w.Id))
            .ExecuteDeleteAsync();
    }

    public async Task<Transition?> GetTransitionWithDetailsAsync(int transitionId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Transition>()
            .Include(t => t.FromStatus)
                .ThenInclude(ws => ws.Status)
            .Include(t => t.ToStatus)
                .ThenInclude(ws => ws.Status)
            .Include(t => t.TransitionFields)
            .FirstOrDefaultAsync(t => t.Id == transitionId);
    }

    public async Task<WorkflowStatus?> GetWorkflowStatusAsync(int workflowId, int statusId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<WorkflowStatus>()
            .FirstOrDefaultAsync(ws => ws.WorkflowId == workflowId && ws.StatusId == statusId);
    }
}