using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using System.Threading;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows;

public partial class Workflows : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IWorkflowService WorkflowService { get; set; } = default!;
    [Inject] private IProjectService ProjectService { get; set; } = default!;

    private List<WorkflowDto> workflows = new();
    private HashSet<int> selectedWorkflowIds = new();
    private HashSet<int> deletingWorkflowIds = new();

    private CancellationTokenSource? _searchCts;

    private List<ProjectDto> availableProjects = new();
    private List<StatusDto> availableStatuses = new();

    private List<int> selectedFilterProjectIds = new();
    private List<int> selectedFilterStatusIds = new();

    private List<int> myCustomOptions = new() { 8, 16, 24, 32 };

    private string searchTerm = string.Empty;
    private int pageSize = 8;
    private int currentPage = 1;
    private int totalWorkflows = 0;
    private bool isLoading = true;

    // متغیرهای مدیریت مودال حذف
    private bool isDeleteModalOpen = false;
    private string deleteModalDescription = string.Empty;
    private WorkflowDto? workflowToDelete = null;
    private bool isBulkDelete = false;

    protected override async Task OnInitializedAsync()
    {
        // دریافت اطلاعات به صورت متوالی برای جلوگیری از تداخل DbContext
        availableProjects = (await ProjectService.GetProjectsAsync()).ToList();
        availableStatuses = await WorkflowService.GetAllStatusesAsync();

        await LoadWorkflows();
    }

    private async Task LoadWorkflows()
    {
        isLoading = true;
        try
        {
            var allWorkflows = (await WorkflowService.GetAllAsync()).AsEnumerable();

            // انتساب پروژه‌ها
            foreach (var w in allWorkflows)
                w.Projects = availableProjects.Where(p => p.WorkflowId == w.Id).ToList();

            // فیلتر جستجو
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (w.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // فیلتر پروژه‌ها
            if (selectedFilterProjectIds.Any())
            {
                var targetWorkflowIds = availableProjects
                    .Where(p => selectedFilterProjectIds.Contains(p.Id) && p.WorkflowId.HasValue)
                    .Select(p => p.WorkflowId!.Value)
                    .ToHashSet();

                allWorkflows = allWorkflows.Where(w => targetWorkflowIds.Contains(w.Id));
            }

            // فیلتر وضعیت‌ها
            if (selectedFilterStatusIds.Any())
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.WorkflowStatuses.Any(ws => selectedFilterStatusIds.Contains(ws.StatusId)));
            }

            // اعمال صفحه‌بندی بهینه
            totalWorkflows = allWorkflows.Count();
            currentPage = totalWorkflows == 0 ? 1 : Math.Min(currentPage, (int)Math.Ceiling((double)totalWorkflows / pageSize));

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

    private void OpenEditWorkflow(WorkflowDto workflow) => Navigation.NavigateTo($"/workflows/editor/{workflow.Id}");

    private void DeleteWorkflow(WorkflowDto workflow)
    {
        workflowToDelete = workflow;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف جریان کاری '{workflow.Name}' اطمینان دارید؟";
        isDeleteModalOpen = true;
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        workflowToDelete = null;
        deleteModalDescription = $"آیا از حذف {selectedWorkflowIds.Count} جریان کاری انتخاب شده اطمینان دارید؟";
        isDeleteModalOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        isDeleteModalOpen = false;

        if (isBulkDelete)
        {
            foreach (var id in selectedWorkflowIds)
            {
                await WorkflowService.DeleteAsync(id);
            }
            selectedWorkflowIds.Clear();
        }
        else if (workflowToDelete != null)
        {
            deletingWorkflowIds.Add(workflowToDelete.Id);
            StateHasChanged();

            await WorkflowService.DeleteAsync(workflowToDelete.Id);

            deletingWorkflowIds.Remove(workflowToDelete.Id);
            // حذف از لیست انتخاب شده‌ها برای بروزرسانی تعداد
            selectedWorkflowIds.Remove(workflowToDelete.Id);
        }

        await LoadWorkflows();
    }

    private void CancelDelete()
    {
        isDeleteModalOpen = false;
        workflowToDelete = null;
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

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(400, token);

            if (!token.IsCancellationRequested)
            {
                await LoadWorkflows();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task OnPageSizeChanged(int newSize)
    {
        pageSize = newSize;
        currentPage = 1;
        await LoadWorkflows();
    }
}