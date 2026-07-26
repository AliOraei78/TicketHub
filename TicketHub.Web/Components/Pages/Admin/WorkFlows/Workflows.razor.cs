using Microsoft.AspNetCore.Components;
using TicketHub.Core.Entities;
using TicketHub.Application.Interfaces;
using TicketHub.Infrastructure.Repositories;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows;


public partial class Workflows : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IWorkflowRepository WorkflowRepository { get; set; } = default!;

    private List<Workflow> workflows = new();
    private HashSet<int> selectedWorkflowIds = new();
    private HashSet<int> deletingWorkflowIds = new();

    private List<Project> availableProjects = new();
    private List<Status> availableStatuses = new();

    private List<int> selectedFilterProjectIds = new();
    private List<int> selectedFilterStatusIds = new();

    private List<int> myCustomOptions = new() { 8, 16, 24, 32 };

    private string searchTerm = string.Empty;
    private int pageSize = 8;
    private int currentPage = 1;
    private int totalWorkflows = 0;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        availableProjects = await WorkflowRepository.GetProjectsAsync();
        availableStatuses = await WorkflowRepository.GetAllStatusesAsync();
        await LoadWorkflows();
    }

    private async Task LoadWorkflows()
    {
        isLoading = true;
        try
        {
            var allWorkflows = await WorkflowRepository.GetAllWithIncludesAsync(
                w => w.Projects,
                w => w.WorkflowStatuses,
                w => w.Transitions // این خط اضافه شد
            );

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allWorkflows = allWorkflows.Where(w => w.Name.Contains(searchTerm) ||
                                                     (w.Description != null && w.Description.Contains(searchTerm)));
            }

            if (selectedFilterProjectIds.Any())
            {
                allWorkflows = allWorkflows.Where(w => w.Projects.Any(p => selectedFilterProjectIds.Contains(p.Id)));
            }

            if (selectedFilterStatusIds.Any())
            {
                allWorkflows = allWorkflows.Where(w => w.WorkflowStatuses.Any(s => selectedFilterStatusIds.Contains(s.StatusId)));
            }

            totalWorkflows = allWorkflows.Count();

            workflows = allWorkflows
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        finally
        {
            isLoading = false;
        }
    }
    private void OpenCreateWorkflow() => Navigation.NavigateTo("/workflows/editor");

    private void ClearSelection() => selectedWorkflowIds.Clear();

    private async Task FilterProjectsChanged(List<int> v) { selectedFilterProjectIds = v; currentPage = 1; await LoadWorkflows(); }
    private async Task FilterStatusesChanged(List<int> v) { selectedFilterStatusIds = v; currentPage = 1; await LoadWorkflows(); }

    private void OpenEditWorkflow(Workflow workflow) => Navigation.NavigateTo($"/workflows/editor/{workflow.Id}");

    private async Task DeleteWorkflow(Workflow workflow)
    {
        deletingWorkflowIds.Add(workflow.Id);
        StateHasChanged();

        await WorkflowRepository.DeleteAsync(workflow.Id);

        deletingWorkflowIds.Remove(workflow.Id);
        await LoadWorkflows();
    }

    private async Task OpenBulkDeleteModal()
    {
        foreach (var id in selectedWorkflowIds)
        {
            await WorkflowRepository.DeleteAsync(id);
        }

        selectedWorkflowIds.Clear();
        await LoadWorkflows();
    }

    private async Task NextPage()
    {
        if (currentPage * pageSize < totalWorkflows)
        {
            currentPage++;
            await LoadWorkflows();
        }
    }

    private async Task PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await LoadWorkflows();
        }
    }

    private async Task OnSearchTermChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        currentPage = 1;
        await LoadWorkflows();
    }

    private async Task OnPageSizeChanged(int newSize)
    {
        pageSize = newSize;
        currentPage = 1;
        await LoadWorkflows();
    }
}
