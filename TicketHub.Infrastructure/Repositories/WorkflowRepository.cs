// TicketHub.Infrastructure/Repositories/WorkflowRepository.cs
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TicketHub.Application.Interfaces;
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
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public override async Task DeleteAsync(int id)
    {
        var entity = await _dbSet
            .Include(w => w.Transitions)
            .Include(w => w.WorkflowStatuses)
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

}