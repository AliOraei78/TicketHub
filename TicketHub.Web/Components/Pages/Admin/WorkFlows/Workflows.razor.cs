using Fluxor;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows;

public partial class Workflows : IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] public IState<WorkflowState> WfState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    private CancellationTokenSource? _searchCts;

    // UI Transient States
    private HashSet<int> selectedWorkflowIds = new();
    private HashSet<int> deletingWorkflowIds = new();

    // Telemetry
    private int TotalWorkflowsCount => WfState.Value.TotalWorkflows;
    private int TotalStatusesCount => WfState.Value.Workflows.Sum(w => w.WorkflowStatuses?.Count ?? 0);
    private int TotalTransitionsCount => WfState.Value.Workflows.Sum(w => w.Transitions?.Count ?? 0);
    private int AssignedProjectsCount => WfState.Value.Workflows.SelectMany(w => w.Projects ?? new List<ProjectDto>()).DistinctBy(p => p.Id).Count();
    private int ActiveWorkflowsCount => WfState.Value.Workflows.Count(w => (w.WorkflowStatuses?.Count ?? 0) > 0);

    private bool isDeleteModalOpen = false;
    private string deleteModalDescription = string.Empty;
    private WorkflowDto? workflowToDelete = null;
    private bool isBulkDelete = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadWorkflowInitialDataAction());
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }

    private void OpenCreateWorkflow() => Navigation.NavigateTo("/workflows/editor");
    private void OpenEditWorkflow(WorkflowDto workflow) => Navigation.NavigateTo($"/workflows/editor/{workflow.Id}");

    private void ClearSelection() => selectedWorkflowIds.Clear();

    private void FilterProjectsChanged(List<int> v)
    {
        Dispatcher.Dispatch(new SetWorkflowFiltersAction(null, null, 1, v, null));
        Dispatcher.Dispatch(new LoadWorkflowsAction());
    }

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

    private void ConfirmDeleteAsync()
    {
        isDeleteModalOpen = false;

        if (isBulkDelete)
        {
            // استخراج نام جریان‌های کاری انتخاب شده برای نمایش در پیام تایید
            var selectedNames = WfState.Value.Workflows
                .Where(w => selectedWorkflowIds.Contains(w.Id))
                .Select(w => w.Name)
                .ToList();

            // ایجاد نسخه کپی با ToList() برای جلوگیری از پاک شدن داده‌ها قبل از اجرای اکشن
            Dispatcher.Dispatch(new DeleteMultipleWorkflowsAction(selectedWorkflowIds.ToList(), selectedNames));
            selectedWorkflowIds.Clear();
        }
        else if (workflowToDelete != null)
        {
            deletingWorkflowIds.Add(workflowToDelete.Id);

            // رفع مشکل: اگر آیتم در لیست انتخاب شده‌ها بود، آن را خارج کن تا شمارشگر بالا آپدیت شود
            if (selectedWorkflowIds.Contains(workflowToDelete.Id))
            {
                selectedWorkflowIds.Remove(workflowToDelete.Id);
                selectedWorkflowIds = new HashSet<int>(selectedWorkflowIds);
            }

            Dispatcher.Dispatch(new DeleteWorkflowAction(workflowToDelete.Id, workflowToDelete.Name));

            _ = Task.Delay(400).ContinueWith(_ => InvokeAsync(() => deletingWorkflowIds.Clear()));
        }
    }

    private void CancelDelete()
    {
        isDeleteModalOpen = false;
        workflowToDelete = null;
    }

    private void NextPage()
    {
        if (WfState.Value.CurrentPage * WfState.Value.PageSize < WfState.Value.TotalWorkflows)
        {
            Dispatcher.Dispatch(new SetWorkflowFiltersAction(null, null, WfState.Value.CurrentPage + 1, null, null));
            Dispatcher.Dispatch(new LoadWorkflowsAction());
        }
    }

    private void PreviousPage()
    {
        if (WfState.Value.CurrentPage > 1)
        {
            Dispatcher.Dispatch(new SetWorkflowFiltersAction(null, null, WfState.Value.CurrentPage - 1, null, null));
            Dispatcher.Dispatch(new LoadWorkflowsAction());
        }
    }

    private async Task OnSearchTermChanged(string newSearchTerm)
    {
        Dispatcher.Dispatch(new SetWorkflowFiltersAction(newSearchTerm, null, 1, null, null));

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(400, token);
            if (!token.IsCancellationRequested)
                Dispatcher.Dispatch(new LoadWorkflowsAction());
        }
        catch (TaskCanceledException) { }
    }

    private void OnPageSizeChanged(int newSize)
    {
        Dispatcher.Dispatch(new SetWorkflowFiltersAction(null, newSize, 1, null, null));
        Dispatcher.Dispatch(new LoadWorkflowsAction());
    }
}