using Fluxor;
using Microsoft.Extensions.Logging;
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
    IEnumerable<CategoryDto> AvailableCategories,
    string SearchTerm,
    int PageSize,
    int CurrentPage,
    List<int> SelectedFilterProjectIds,
    List<int> SelectedFilterStatusIds,
    string? FormErrorMessage,
    IEnumerable<TicketFieldDto> DynamicFields)
{
    private TicketState() : this(true, Array.Empty<TicketDto>(), 0, Array.Empty<ProjectDto>(), Array.Empty<StatusDto>(), Array.Empty<PriorityDto>(), Array.Empty<CategoryDto>(), string.Empty, 10, 1, new(), new(), null, Array.Empty<TicketFieldDto>()) { }
}

// 2. Actions
public record LoadTicketInitialDataAction(IEnumerable<string> UserRoles);
public record TicketInitialDataLoadedAction(IEnumerable<ProjectDto> Projects, IEnumerable<StatusDto> Statuses, IEnumerable<PriorityDto> Priorities, IEnumerable<CategoryDto> Categories);
public record LoadTicketsAction();
public record TicketsLoadedAction(IEnumerable<TicketDto> Tickets, int TotalCount, int ValidatedPage);
public record SetTicketFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, List<int>? ProjectIds, List<int>? StatusIds);
public record SaveTicketAction(TicketDto Ticket);
public record SaveTicketSuccessAction();
public record SaveTicketFailedAction(string ErrorMessage);
public record ClearTicketMessagesAction();
public record LoadDynamicFieldsAction(int CategoryId);
public record DynamicFieldsLoadedAction(IEnumerable<TicketFieldDto> Fields);

// 3. Reducers
public static class TicketReducers
{
    [ReducerMethod]
    public static TicketState ReduceLoadTickets(TicketState state, LoadTicketsAction action) => state with { IsLoading = true };

    [ReducerMethod]
    public static TicketState ReduceInitialDataLoaded(TicketState state, TicketInitialDataLoadedAction action) =>
                state with { AvailableProjects = action.Projects, AvailableStatuses = action.Statuses, AvailablePriorities = action.Priorities, AvailableCategories = action.Categories };

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

    [ReducerMethod]
    public static TicketState ReduceDynamicFieldsLoaded(TicketState state, DynamicFieldsLoadedAction action) =>
    state with { DynamicFields = action.Fields };
}

// 4. Effects
public class TicketEffects
{
    private readonly ITicketService _ticketService;
    private readonly IProjectService _projectService;
    private readonly IStatusService _statusService;
    private readonly IPriorityService _priorityService;
    private readonly ICategoryService _categoryService;
    private readonly IState<TicketState> _state;
    private readonly ITicketFieldService _ticketFieldService;
    private readonly ILogger<TicketEffects> _logger;

    public TicketEffects(
        ITicketService ticketService,
        IProjectService projectService,
        IStatusService statusService,
        IPriorityService priorityService,
        ICategoryService categoryService,
        IState<TicketState> state,
        ITicketFieldService ticketFieldService,
        ILogger<TicketEffects> logger)
    {
        _ticketService = ticketService;
        _projectService = projectService;
        _statusService = statusService;
        _priorityService = priorityService;
        _categoryService = categoryService;
        _state = state;
        _ticketFieldService = ticketFieldService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadInitialData(LoadTicketInitialDataAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی اطلاعات اولیه تیکت‌ها.");

            var projects = await _projectService.GetProjectsByUserRolesAsync(action.UserRoles);
            var statuses = await _statusService.GetAllAsync();
            var priorities = await _priorityService.GetAllAsync();
            var categories = await _categoryService.GetCategoriesByUserRolesAsync(action.UserRoles);

            dispatcher.Dispatch(new TicketInitialDataLoadedAction(projects, statuses, priorities, categories));
            dispatcher.Dispatch(new LoadTicketsAction());

            _logger.LogInformation("اطلاعات اولیه تیکت‌ها با موفقیت بارگذاری شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه تیکت‌ها.");
        }
    }

    [EffectMethod(typeof(LoadTicketsAction))]
    public async Task HandleLoadTickets(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست تیکت‌ها.");

            var st = _state.Value;
            var result = await _ticketService.GetFilteredTicketsAsync(string.Empty, null, null, null, 1, 1000);

            dispatcher.Dispatch(new TicketsLoadedAction(result.Tickets, result.TotalCount, 1));

            _logger.LogInformation("لیست تیکت‌ها با موفقیت دریافت شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست تیکت‌ها.");
        }
    }

    [EffectMethod]
    public async Task HandleLoadDynamicFields(LoadDynamicFieldsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی فیلدهای داینامیک برای دسته‌بندی با شناسه {CategoryId}.", action.CategoryId);

            var fields = await _ticketFieldService.GetFieldsByCategoryIdAsync(action.CategoryId);
            dispatcher.Dispatch(new DynamicFieldsLoadedAction(fields));

            _logger.LogInformation("فیلدهای داینامیک با موفقیت دریافت شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت فیلدهای داینامیک برای دسته‌بندی {CategoryId}.", action.CategoryId);
        }
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
            _logger.LogInformation("اجرای اکشن SaveTicketAction برای {ActionType} تیکت.", ticketModel.Id == 0 ? "ایجاد" : "ویرایش");

            if (ticketModel.Id == 0)
                await _ticketService.CreateAsync(ticketModel);
            else
                await _ticketService.UpdateAsync(ticketModel);

            dispatcher.Dispatch(new SaveTicketSuccessAction());
            dispatcher.Dispatch(new LoadTicketsAction());

            _logger.LogInformation("تیکت با موفقیت {ActionType} شد.", ticketModel.Id == 0 ? "ایجاد" : "ویرایش");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} تیکت.", ticketModel.Id == 0 ? "ایجاد" : "ویرایش");
            dispatcher.Dispatch(new SaveTicketFailedAction("• خطایی در ذخیره تیکت رخ داد."));
        }
    }
}