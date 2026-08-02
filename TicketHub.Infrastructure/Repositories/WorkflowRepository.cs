// TicketHub.Infrastructure/Repositories/WorkflowRepository.cs
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TicketHub.Core.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories;

public class WorkflowRepository : GenericRepository<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Workflow?> GetWorkflowWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(w => w.WorkflowStatuses)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.AllowedRoles)
            .Include(w => w.Transitions)                                // <--- اضافه شود
                .ThenInclude(t => t.TransitionFields)
                .AsSplitQuery()// <--- اضافه شود
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public override async Task DeleteAsync(int id)
    {
        var entity = await _dbSet
            .Include(w => w.Transitions)
            .Include(w => w.WorkflowStatuses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);

        if (entity != null)
        {
            if (entity.Transitions.Any())
            {
                _context.Set<Transition>().RemoveRange(entity.Transitions);
            }

            if (entity.WorkflowStatuses.Any())
            {
                _context.Set<WorkflowStatus>().RemoveRange(entity.WorkflowStatuses);
            }

            _dbSet.Remove(entity);
            await SaveChangesAsync();
        }
    }

    public async Task<List<Status>> GetAllStatusesAsync()
    {
        return await _context.Set<Status>().ToListAsync();
    }

    // به انتهای کلاس WorkflowRepository اضافه کنید
    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _context.Set<Project>().ToListAsync();
    }

    public async Task<List<WorkflowStatus>> GetStatusesAsync()
    {
        return await _context.Set<WorkflowStatus>().ToListAsync();
    }

    public void RemoveTransitionRoles(IEnumerable<TransitionRole> roles)
    {
        _context.Set<TransitionRole>().RemoveRange(roles);
    }

    public void RemoveTransitionFields(IEnumerable<TransitionField> fields)
    {
        _context.Set<TransitionField>().RemoveRange(fields);
    }

    public void RemoveWorkflowStatuses(IEnumerable<WorkflowStatus> statuses)
    {
        _context.Set<WorkflowStatus>().RemoveRange(statuses);
    }

    public void RemoveTransitions(IEnumerable<Transition> transitions)
    {
        _context.Set<Transition>().RemoveRange(transitions);
    }

    public async Task CommitChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Workflow>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(w => w.WorkflowStatuses)
            .Include(w => w.Transitions)
            .AsSplitQuery()
            .ToListAsync();
    }
}