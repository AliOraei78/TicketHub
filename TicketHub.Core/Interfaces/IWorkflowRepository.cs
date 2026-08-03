// TicketHub.Core/Interfaces/IWorkflowRepository.cs
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Interfaces;

public interface IWorkflowRepository : IRepository<Workflow>
{
    // متدهای اختصاصی Workflow اینجا قرار می‌گیرند (مثلاً واکشی با تمام Include ها)
    Task<Workflow?> GetWorkflowWithDetailsAsync(int id);
    Task<List<Project>> GetProjectsAsync();
    Task<List<Status>> GetAllStatusesAsync();

    void RemoveTransitionRoles(IEnumerable<TransitionRole> roles);
    void RemoveTransitionFields(IEnumerable<TransitionField> fields);
    void RemoveWorkflowStatuses(IEnumerable<WorkflowStatus> statuses);
    void RemoveTransitions(IEnumerable<Transition> transitions);
    Task CommitChangesAsync();
    Task<List<Workflow>> GetAllWithDetailsAsync();
    Task DeleteRangeAsync(IEnumerable<int> ids);
}