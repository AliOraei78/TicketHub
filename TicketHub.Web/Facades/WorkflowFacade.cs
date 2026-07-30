using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class WorkflowFacade
{
    private readonly IWorkflowService _workflowService;
    private readonly IProjectService _projectService;
    public readonly WorkflowStateContainer State;

    public WorkflowFacade(
        IWorkflowService workflowService,
        IProjectService projectService,
        WorkflowStateContainer state)
    {
        _workflowService = workflowService;
        _projectService = projectService;
        State = state;
    }

    public async Task InitializeAsync()
    {
        State.AvailableProjects = (await _projectService.GetProjectsAsync()).ToList();
        State.AvailableStatuses = await _workflowService.GetAllStatusesAsync();
        await LoadWorkflowsAsync();
    }

    public async Task LoadWorkflowsAsync()
    {
        State.IsLoading = true;
        State.NotifyStateChanged();

        try
        {
            var allWorkflows = (await _workflowService.GetAllAsync()).AsEnumerable();

            foreach (var w in allWorkflows)
                w.Projects = State.AvailableProjects.Where(p => p.WorkflowId == w.Id).ToList();

            if (!string.IsNullOrWhiteSpace(State.SearchTerm))
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.Name.Contains(State.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (w.Description?.Contains(State.SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (State.SelectedFilterProjectIds.Any())
            {
                var targetWorkflowIds = State.AvailableProjects
                    .Where(p => State.SelectedFilterProjectIds.Contains(p.Id) && p.WorkflowId.HasValue)
                    .Select(p => p.WorkflowId!.Value)
                    .ToHashSet();
                allWorkflows = allWorkflows.Where(w => targetWorkflowIds.Contains(w.Id));
            }

            if (State.SelectedFilterStatusIds.Any())
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.WorkflowStatuses.Any(ws => State.SelectedFilterStatusIds.Contains(ws.StatusId)));
            }

            State.TotalWorkflows = allWorkflows.Count();
            State.CurrentPage = State.TotalWorkflows == 0 ? 1 : Math.Min(State.CurrentPage, (int)Math.Ceiling((double)State.TotalWorkflows / State.PageSize));

            State.Workflows = allWorkflows
                .Skip((State.CurrentPage - 1) * State.PageSize)
                .Take(State.PageSize)
                .ToList();
        }
        finally
        {
            State.IsLoading = false;
            State.NotifyStateChanged();
        }
    }

    public async Task DeleteWorkflowAsync(int id)
    {
        await _workflowService.DeleteAsync(id);
        State.SelectedWorkflowIds.Remove(id);
        await LoadWorkflowsAsync();
    }

    public async Task BulkDeleteAsync()
    {
        foreach (var id in State.SelectedWorkflowIds)
        {
            await _workflowService.DeleteAsync(id);
        }
        State.SelectedWorkflowIds.Clear();
        await LoadWorkflowsAsync();
    }
}
