using TicketHub.Core.Entities;

namespace TicketHub.Core.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetProjectWithWorkflowAsync(int projectId);
}