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
}