using TicketHub.Application.DTOs;

namespace TicketHub.Web.States;

public class WorkflowStateContainer
{
    public List<WorkflowDto> Workflows { get; set; } = new();
    public HashSet<int> SelectedWorkflowIds { get; set; } = new();
    public HashSet<int> DeletingWorkflowIds { get; set; } = new();

    public List<ProjectDto> AvailableProjects { get; set; } = new();
    public List<StatusDto> AvailableStatuses { get; set; } = new();

    public List<int> SelectedFilterProjectIds { get; set; } = new();
    public List<int> SelectedFilterStatusIds { get; set; } = new();

    public List<int> MyCustomOptions { get; set; } = new() { 8, 16, 24, 32 };

    public string SearchTerm { get; set; } = string.Empty;
    public int PageSize { get; set; } = 8;
    public int CurrentPage { get; set; } = 1;
    public int TotalWorkflows { get; set; } = 0;
    public bool IsLoading { get; set; } = true;

    // متغیرهای مدیریت مودال حذف
    public bool IsDeleteModalOpen { get; set; } = false;
    public string DeleteModalDescription { get; set; } = string.Empty;
    public WorkflowDto? WorkflowToDelete { get; set; } = null;
    public bool IsBulkDelete { get; set; } = false;

    // رویداد برای اطلاع‌رسانی تغییرات به کامپوننت
    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}
