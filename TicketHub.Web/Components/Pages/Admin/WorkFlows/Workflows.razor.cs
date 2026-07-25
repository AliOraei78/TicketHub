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

    private string searchTerm = string.Empty;
    private int pageSize = 10;
    private int currentPage = 1;
    private int totalWorkflows = 0;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadWorkflows();
    }

    private async Task LoadWorkflows()
    {
        isLoading = true;
        try
        {
            var allWorkflows = await WorkflowRepository.GetAllWithIncludesAsync(
                w => w.Projects,
                w => w.WorkflowStatuses
            );

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allWorkflows = allWorkflows.Where(w => w.Name.Contains(searchTerm) ||
                                                     (w.Description != null && w.Description.Contains(searchTerm)));
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
}
