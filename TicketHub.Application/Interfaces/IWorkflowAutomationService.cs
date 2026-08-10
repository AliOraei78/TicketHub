using System.Threading.Tasks;

namespace TicketHub.Application.Interfaces;

public interface IWorkflowAutomationService
{
    Task ProcessAutomaticTransitionsAsync();
    Task ProcessDeadlinesAsync();
    Task TriggerImmediateAutomaticTransitionsAsync(int ticketId, int maxDepth = 5);
}
