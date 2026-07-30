using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Facades; // فضای نام Facade را تنظیم کنید
using System.Threading;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows;

public partial class Workflows : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] public WorkflowFacade Facade { get; set; } = default!; // اضافه شدن Facade

    private CancellationTokenSource? _searchCts;

    protected override async Task OnInitializedAsync()
    {
        Facade.State.OnChange += StateHasChanged;
        await Facade.InitializeAsync();
    }

    public void Dispose() => Facade.State.OnChange -= StateHasChanged;

    private void OpenCreateWorkflow() => Navigation.NavigateTo("/workflows/editor");

    private void OpenEditWorkflow(WorkflowDto workflow) => Navigation.NavigateTo($"/workflows/editor/{workflow.Id}");

    private void ClearSelection() => Facade.State.SelectedWorkflowIds.Clear();

    private async Task FilterProjectsChanged(List<int> v)
    {
        Facade.State.SelectedFilterProjectIds = v;
        Facade.State.CurrentPage = 1;
        await Facade.LoadWorkflowsAsync();
    }

    private void DeleteWorkflow(WorkflowDto workflow)
    {
        Facade.State.WorkflowToDelete = workflow;
        Facade.State.IsBulkDelete = false;
        Facade.State.DeleteModalDescription = $"آیا از حذف جریان کاری '{workflow.Name}' اطمینان دارید؟";
        Facade.State.IsDeleteModalOpen = true;
    }

    private void OpenBulkDeleteModal()
    {
        Facade.State.IsBulkDelete = true;
        Facade.State.WorkflowToDelete = null;
        Facade.State.DeleteModalDescription = $"آیا از حذف {Facade.State.SelectedWorkflowIds.Count} جریان کاری انتخاب شده اطمینان دارید؟";
        Facade.State.IsDeleteModalOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        Facade.State.IsDeleteModalOpen = false;

        if (Facade.State.IsBulkDelete)
            await Facade.BulkDeleteAsync();
        else if (Facade.State.WorkflowToDelete != null)
        {
            Facade.State.DeletingWorkflowIds.Add(Facade.State.WorkflowToDelete.Id);
            await Facade.DeleteWorkflowAsync(Facade.State.WorkflowToDelete.Id);
            Facade.State.DeletingWorkflowIds.Remove(Facade.State.WorkflowToDelete.Id);
        }
    }

    private void CancelDelete()
    {
        Facade.State.IsDeleteModalOpen = false;
        Facade.State.WorkflowToDelete = null;
    }

    private async Task NextPage()
    {
        if (Facade.State.CurrentPage * Facade.State.PageSize < Facade.State.TotalWorkflows)
        {
            Facade.State.CurrentPage++;
            await Facade.LoadWorkflowsAsync();
        }
    }

    private async Task PreviousPage()
    {
        if (Facade.State.CurrentPage > 1)
        {
            Facade.State.CurrentPage--;
            await Facade.LoadWorkflowsAsync();
        }
    }

    private async Task OnSearchTermChanged(string newSearchTerm)
    {
        Facade.State.SearchTerm = newSearchTerm;
        Facade.State.CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(400, token);
            if (!token.IsCancellationRequested) await Facade.LoadWorkflowsAsync();
        }
        catch (TaskCanceledException) { }
    }

    private async Task OnPageSizeChanged(int newSize)
    {
        Facade.State.PageSize = newSize;
        Facade.State.CurrentPage = 1;
        await Facade.LoadWorkflowsAsync();
    }
}