using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record TicketState(
    bool IsLoading,
    IEnumerable<TicketDto> Tickets,
    int TotalTickets,
    IEnumerable<ProjectDto> AvailableProjects,
    IEnumerable<StatusDto> AvailableStatuses,
    IEnumerable<PriorityDto> AvailablePriorities,
    string SearchTerm,
    int PageSize,
    int CurrentPage,
    List<int> SelectedFilterProjectIds,
    List<int> SelectedFilterStatusIds,
    string? FormErrorMessage)
{
    private TicketState() : this(true, Array.Empty<TicketDto>(), 0, Array.Empty<ProjectDto>(), Array.Empty<StatusDto>(), Array.Empty<PriorityDto>(), string.Empty, 10, 1, new(), new(), null) { }
}

// 2. Actions
public record LoadTicketInitialDataAction();
public record TicketInitialDataLoadedAction(IEnumerable<ProjectDto> Projects, IEnumerable<StatusDto> Statuses, IEnumerable<PriorityDto> Priorities);
public record LoadTicketsAction();
public record TicketsLoadedAction(IEnumerable<TicketDto> Tickets, int TotalCount, int ValidatedPage);
public record SetTicketFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, List<int>? ProjectIds, List<int>? StatusIds);
public record SaveTicketAction(TicketDto Ticket);
public record SaveTicketSuccessAction();
public record SaveTicketFailedAction(string ErrorMessage);
public record ClearTicketMessagesAction();

// 3. Reducers
public static class TicketReducers
{
    [ReducerMethod]
    public static TicketState ReduceLoadTickets(TicketState state, LoadTicketsAction action) => state with { IsLoading = true };

    [ReducerMethod]
    public static TicketState ReduceInitialDataLoaded(TicketState state, TicketInitialDataLoadedAction action) =>
            state with { AvailableProjects = action.Projects, AvailableStatuses = action.Statuses, AvailablePriorities = action.Priorities };

    [ReducerMethod]
    public static TicketState ReduceTicketsLoaded(TicketState state, TicketsLoadedAction action) =>
        state with { IsLoading = false, Tickets = action.Tickets, TotalTickets = action.TotalCount, CurrentPage = action.ValidatedPage };

    [ReducerMethod]
    public static TicketState ReduceSetFilters(TicketState state, SetTicketFiltersAction action) =>
        state with
        {
            SearchTerm = action.SearchTerm ?? state.SearchTerm,
            PageSize = action.PageSize ?? state.PageSize,
            CurrentPage = action.CurrentPage ?? state.CurrentPage,
            SelectedFilterProjectIds = action.ProjectIds ?? state.SelectedFilterProjectIds,
            SelectedFilterStatusIds = action.StatusIds ?? state.SelectedFilterStatusIds
        };

    [ReducerMethod]
    public static TicketState ReduceSaveFailed(TicketState state, SaveTicketFailedAction action) => state with { FormErrorMessage = action.ErrorMessage };

    [ReducerMethod(typeof(SaveTicketSuccessAction))]
    public static TicketState ReduceSaveSuccess(TicketState state) => state with { FormErrorMessage = null };

    [ReducerMethod(typeof(ClearTicketMessagesAction))]
    public static TicketState ReduceClearMessages(TicketState state) => state with { FormErrorMessage = null };
}

// 4. Effects
public class TicketEffects
{
    private readonly ITicketService _ticketService;
    private readonly IProjectService _projectService;
    private readonly IStatusService _statusService;
    private readonly IPriorityService _priorityService;
    private readonly IState<TicketState> _state;

    public TicketEffects(ITicketService ticketService, IProjectService projectService, IStatusService statusService, IPriorityService priorityService, IState<TicketState> state)
    {
        _ticketService = ticketService;
        _projectService = projectService;
        _statusService = statusService;
        _priorityService = priorityService;
        _state = state;
    }

    [EffectMethod(typeof(LoadTicketInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        var projects = await _projectService.GetProjectsAsync();
        var statuses = await _statusService.GetAllAsync();
        var priorities = await _priorityService.GetAllAsync();

        dispatcher.Dispatch(new TicketInitialDataLoadedAction(projects, statuses, priorities));
        dispatcher.Dispatch(new LoadTicketsAction());
    }

    [EffectMethod(typeof(LoadTicketsAction))]
    public async Task HandleLoadTickets(IDispatcher dispatcher)
    {
        var st = _state.Value;

        // فراخوانی سرویس (همانند قبل دیتای خام دریافت می‌شود تا فیلترها در UI یا با متد جدید سرور هندل شوند)
        var result = await _ticketService.GetFilteredTicketsAsync(string.Empty, null, null, null, 1, 1000);

        dispatcher.Dispatch(new TicketsLoadedAction(result.Tickets, result.TotalCount, 1));
    }

    [EffectMethod]
    public async Task HandleSaveTicket(SaveTicketAction action, IDispatcher dispatcher)
    {
        var errors = new List<string>();
        var ticketModel = action.Ticket;

        if (string.IsNullOrWhiteSpace(ticketModel.Title)) errors.Add("• عنوان تیکت الزامی است.");
        if (string.IsNullOrWhiteSpace(ticketModel.Description)) errors.Add("• توضیحات تیکت الزامی است.");
        if (ticketModel.ProjectId == 0) errors.Add("• انتخاب پروژه الزامی است.");

        if (errors.Any())
        {
            dispatcher.Dispatch(new SaveTicketFailedAction(string.Join("\n", errors)));
            return;
        }

        try
        {
            if (ticketModel.Id == 0)
                await _ticketService.CreateAsync(ticketModel);
            else
                await _ticketService.UpdateAsync(ticketModel);

            dispatcher.Dispatch(new SaveTicketSuccessAction());
            dispatcher.Dispatch(new LoadTicketsAction());
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new SaveTicketFailedAction("• خطایی در ذخیره تیکت رخ داد."));
        }
    }
}