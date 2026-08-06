using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

    public async Task<Project?> GetProjectWithWorkflowAsync(int projectId)
    {
        using var context = await _factory.CreateDbContextAsync();

        return await context.Set<Project>()
            .Include(p => p.Workflow)
            .ThenInclude(w => w.WorkflowStatuses)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }
}