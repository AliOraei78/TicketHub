using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record WorkflowState(
    bool IsLoading,
    IEnumerable<WorkflowDto> Workflows,
    int TotalWorkflows,
    IEnumerable<ProjectDto> AvailableProjects,
    IEnumerable<StatusDto> AvailableStatuses,
    string SearchTerm,
    int PageSize,
    int CurrentPage,
    List<int> SelectedFilterProjectIds,
    List<int> SelectedFilterStatusIds,
    List<int> MyCustomOptions)
{
    private WorkflowState() : this(true, Array.Empty<WorkflowDto>(), 0, Array.Empty<ProjectDto>(), Array.Empty<StatusDto>(), string.Empty, 8, 1, new(), new(), new() { 8, 16, 24, 32 }) { }
}

// 2. Actions
public record LoadWorkflowInitialDataAction();
public record WorkflowInitialDataLoadedAction(IEnumerable<ProjectDto> Projects, IEnumerable<StatusDto> Statuses);
public record LoadWorkflowsAction();
public record WorkflowsLoadedAction(IEnumerable<WorkflowDto> Workflows, int TotalCount, int ValidatedPage);
public record SetWorkflowFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, List<int>? ProjectIds, List<int>? StatusIds);
public record DeleteWorkflowAction(int Id);
public record DeleteMultipleWorkflowsAction(IEnumerable<int> Ids);

// 3. Reducers
public static class WorkflowReducers
{
    [ReducerMethod]
    public static WorkflowState ReduceLoadWorkflows(WorkflowState state, LoadWorkflowsAction action) => state with { IsLoading = true };

    [ReducerMethod]
    public static WorkflowState ReduceInitialDataLoaded(WorkflowState state, WorkflowInitialDataLoadedAction action) =>
        state with { AvailableProjects = action.Projects, AvailableStatuses = action.Statuses };

    [ReducerMethod]
    public static WorkflowState ReduceWorkflowsLoaded(WorkflowState state, WorkflowsLoadedAction action) =>
        state with { IsLoading = false, Workflows = action.Workflows, TotalWorkflows = action.TotalCount, CurrentPage = action.ValidatedPage };

    [ReducerMethod]
    public static WorkflowState ReduceSetFilters(WorkflowState state, SetWorkflowFiltersAction action) =>
        state with
        {
            SearchTerm = action.SearchTerm ?? state.SearchTerm,
            PageSize = action.PageSize ?? state.PageSize,
            CurrentPage = action.CurrentPage ?? state.CurrentPage,
            SelectedFilterProjectIds = action.ProjectIds ?? state.SelectedFilterProjectIds,
            SelectedFilterStatusIds = action.StatusIds ?? state.SelectedFilterStatusIds
        };
}

// 4. Effects
public class WorkflowEffects
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IState<WorkflowState> _state;
    private readonly ILogger<WorkflowEffects> _logger;

    public WorkflowEffects(
        IServiceScopeFactory scopeFactory,
        IState<WorkflowState> state,
        ILogger<WorkflowEffects> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _logger = logger;
    }

    [EffectMethod(typeof(LoadWorkflowInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی اطلاعات اولیه جریان‌های کاری.");

            using var scope = _scopeFactory.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

            var projects = await projectService.GetProjectsAsync();
            var statuses = await workflowService.GetAllStatusesAsync();

            dispatcher.Dispatch(new WorkflowInitialDataLoadedAction(projects, statuses));
            dispatcher.Dispatch(new LoadWorkflowsAction());

            _logger.LogInformation("اطلاعات اولیه جریان‌های کاری با موفقیت دریافت شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه جریان‌های کاری.");
        }
    }

    [EffectMethod(typeof(LoadWorkflowsAction))]
    public async Task HandleLoadWorkflows(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست جریان‌های کاری.");

            using var scope = _scopeFactory.CreateScope();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

            var st = _state.Value;
            var allWorkflows = (await workflowService.GetAllAsync()).ToList();

            foreach (var w in allWorkflows)
                w.Projects = st.AvailableProjects.Where(p => p.WorkflowId == w.Id).ToList();

            if (!string.IsNullOrWhiteSpace(st.SearchTerm))
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.Name.Contains(st.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (w.Description?.Contains(st.SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            }

            if (st.SelectedFilterProjectIds.Any())
            {
                var targetWorkflowIds = st.AvailableProjects
                    .Where(p => st.SelectedFilterProjectIds.Contains(p.Id) && p.WorkflowId.HasValue)
                    .Select(p => p.WorkflowId!.Value)
                    .ToHashSet();
                allWorkflows = allWorkflows.Where(w => targetWorkflowIds.Contains(w.Id)).ToList();
            }

            if (st.SelectedFilterStatusIds.Any())
            {
                allWorkflows = allWorkflows.Where(w =>
                    w.WorkflowStatuses.Any(ws => st.SelectedFilterStatusIds.Contains(ws.StatusId))).ToList();
            }

            int totalCount = allWorkflows.Count;
            int maxPage = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / st.PageSize);
            int validPage = st.CurrentPage > maxPage && maxPage > 0 ? maxPage : st.CurrentPage;

            var pagedWorkflows = allWorkflows
                .Skip((validPage - 1) * st.PageSize)
                .Take(st.PageSize)
                .ToList();

            dispatcher.Dispatch(new WorkflowsLoadedAction(pagedWorkflows, totalCount, validPage));

            _logger.LogInformation("لیست جریان‌های کاری با موفقیت دریافت شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست جریان‌های کاری.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteWorkflow(DeleteWorkflowAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف جریان کاری با شناسه {WorkflowId}.", action.Id);

            using var scope = _scopeFactory.CreateScope();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

            await workflowService.DeleteAsync(action.Id);
            dispatcher.Dispatch(new LoadWorkflowsAction());

            _logger.LogInformation("جریان کاری با شناسه {WorkflowId} با موفقیت حذف شد.", action.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف جریان کاری با شناسه {WorkflowId}.", action.Id);
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleWorkflows(DeleteMultipleWorkflowsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی جریان‌های کاری به تعداد {Count}.", count);

            using var scope = _scopeFactory.CreateScope();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

            await workflowService.DeleteRangeAsync(action.Ids);
            dispatcher.Dispatch(new LoadWorkflowsAction());

            _logger.LogInformation("تعداد {Count} جریان کاری با موفقیت حذف شدند.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی جریان‌های کاری.");
        }
    }
}