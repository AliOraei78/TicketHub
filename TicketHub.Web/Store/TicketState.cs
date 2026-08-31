using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

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
    List<int> SelectedFilterPriorityIds,
    IEnumerable<TicketFieldDto> DynamicFields,
    TicketTelemetrySummaryDto Telemetry,
    string SortBy,
    bool IsAscending)
{
    private TicketState() : this(true, Array.Empty<TicketDto>(), 0, Array.Empty<ProjectDto>(), Array.Empty<StatusDto>(), Array.Empty<PriorityDto>(), Array.Empty<CategoryDto>(), string.Empty, 9, 1, new(), new(), new(), Array.Empty<TicketFieldDto>(), new(), "createdAt", false) { }
}

// 2. Actions
public record LoadTicketInitialDataAction(IEnumerable<string> UserRoles);
public record TicketInitialDataLoadedAction(IEnumerable<ProjectDto> Projects, IEnumerable<StatusDto> Statuses, IEnumerable<PriorityDto> Priorities, IEnumerable<CategoryDto> Categories);
public record LoadTicketsAction();
public record TicketsLoadedAction(IEnumerable<TicketDto> Tickets, int TotalCount, int ValidatedPage, TicketTelemetrySummaryDto Telemetry);
public record SetTicketFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, List<int>? ProjectIds, List<int>? StatusIds, List<int>? PriorityIds = null, string? SortBy = null, bool? IsAscending = null);
public record SaveTicketAction(TicketDto Ticket);
public record SaveTicketSuccessAction();
public record SaveTicketFailedAction(string ErrorMessage);
public record ClearTicketMessagesAction();
public record LoadDynamicFieldsAction(int CategoryId);
public record DynamicFieldsLoadedAction(IEnumerable<TicketFieldDto> Fields);
public record DeleteTicketAction(int Id, string TicketTitle);
public record DeleteMultipleTicketsAction(IEnumerable<int> Ids, IEnumerable<string> TicketTitles);

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
        state with { IsLoading = false, Tickets = action.Tickets, TotalTickets = action.TotalCount, CurrentPage = action.ValidatedPage, Telemetry = action.Telemetry };

    [ReducerMethod]
    public static TicketState ReduceSetFilters(TicketState state, SetTicketFiltersAction action) =>
        state with
        {
            SearchTerm = action.SearchTerm ?? state.SearchTerm,
            PageSize = action.PageSize ?? state.PageSize,
            CurrentPage = action.CurrentPage ?? state.CurrentPage,
            SelectedFilterProjectIds = action.ProjectIds ?? state.SelectedFilterProjectIds,
            SelectedFilterStatusIds = action.StatusIds ?? state.SelectedFilterStatusIds,
            SelectedFilterPriorityIds = action.PriorityIds ?? state.SelectedFilterPriorityIds,
            SortBy = action.SortBy ?? state.SortBy,
            IsAscending = action.IsAscending ?? state.IsAscending
        };

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
    private readonly IToastService _toastService;

    public TicketEffects(
        ITicketService ticketService,
        IProjectService projectService,
        IStatusService statusService,
        IPriorityService priorityService,
        ICategoryService categoryService,
        IState<TicketState> state,
        ITicketFieldService ticketFieldService,
        ILogger<TicketEffects> logger,
        IToastService toastService)
    {
        _ticketService = ticketService;
        _projectService = projectService;
        _statusService = statusService;
        _priorityService = priorityService;
        _categoryService = categoryService;
        _state = state;
        _ticketFieldService = ticketFieldService;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadInitialData(LoadTicketInitialDataAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی اطلاعات اولیه تیکت‌ها.");

            var projects = await _projectService.GetProjectsByUserRolesAsync(action.UserRoles);
            var statuses = (await _statusService.GetAllAsync()).Where(s => s.IsActive).ToList();
            var priorities = (await _priorityService.GetAllAsync()).Where(p => p.IsActive).OrderBy(p => p.Level).ToList();
            var categories = await _categoryService.GetCategoriesByUserRolesAsync(action.UserRoles);


            dispatcher.Dispatch(new TicketInitialDataLoadedAction(projects, statuses, priorities, categories));
            dispatcher.Dispatch(new LoadTicketsAction());

            _logger.LogInformation("اطلاعات اولیه تیکت‌ها با موفقیت بارگذاری شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه تیکت‌ها.");
            _toastService.ShowError("خطا در دریافت اطلاعات اولیه. لطفا صفحه را مجدداً بارگذاری کنید.");
        }
    }

    [EffectMethod(typeof(LoadTicketsAction))]
    public async Task HandleLoadTickets(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی لیست تیکت‌ها.");

            var st = _state.Value;
            var result = await _ticketService.GetFilteredTicketsAsync(st.SearchTerm, st.SelectedFilterProjectIds, st.SelectedFilterStatusIds, st.SelectedFilterPriorityIds, null, st.CurrentPage, st.PageSize, null, st.SortBy, st.IsAscending);

            int maxPage = result.TotalCount == 0 ? 1 : (int)Math.Ceiling(result.TotalCount / (double)st.PageSize);
            var finalPage = st.CurrentPage;

            if (st.CurrentPage > maxPage && maxPage > 0)
            {
                finalPage = maxPage;
                result = await _ticketService.GetFilteredTicketsAsync(st.SearchTerm, st.SelectedFilterProjectIds, st.SelectedFilterStatusIds, st.SelectedFilterPriorityIds, null, finalPage, st.PageSize, null, st.SortBy, st.IsAscending);
            }

            var telemetry = await _ticketService.GetTicketTelemetrySummaryAsync(st.SearchTerm, st.SelectedFilterProjectIds, st.SelectedFilterStatusIds, st.SelectedFilterPriorityIds, null);

            dispatcher.Dispatch(new TicketsLoadedAction(result.Tickets, result.TotalCount, finalPage, telemetry));

            _logger.LogInformation("لیست تیکت‌ها با موفقیت دریافت شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست تیکت‌ها.");
            _toastService.ShowError("خطا در دریافت لیست تیکت‌ها.");
        }
    }

    [EffectMethod(typeof(SetTicketFiltersAction))]
    public Task HandleSetFilters(IDispatcher dispatcher)
    {
        dispatcher.Dispatch(new LoadTicketsAction());
        return Task.CompletedTask;
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
            _toastService.ShowError("خطا در دریافت فیلدهای پویا.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveTicket(SaveTicketAction action, IDispatcher dispatcher)
    {
        var ticketModel = action.Ticket;

        try
        {
            _logger.LogInformation("اجرای اکشن SaveTicketAction برای {ActionType} تیکت.", ticketModel.Id == 0 ? "ایجاد" : "ویرایش");

            if (ticketModel.Id == 0)
                await _ticketService.CreateAsync(ticketModel);
            else
                await _ticketService.UpdateAsync(ticketModel);

            _toastService.ShowSuccess(ticketModel.Id == 0 ? "تیکت با موفقیت ایجاد شد." : "تغییرات تیکت با موفقیت ذخیره شد.");

            dispatcher.Dispatch(new SaveTicketSuccessAction());
            dispatcher.Dispatch(new LoadTicketsAction());
        }
        catch (ValidationException ex)
        {
            var errorMessage = ex.Errors != null && ex.Errors.Any()
                ? string.Join("\n", ex.Errors.SelectMany(e => e.Value).Select(msg => $"• {msg}"))
                : $"• {ex.Message}";
            _toastService.ShowWarning(errorMessage, "خطای اطلاعات ورودی");
            dispatcher.Dispatch(new SaveTicketFailedAction(errorMessage));
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
            dispatcher.Dispatch(new SaveTicketFailedAction(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای سیستمی در زمان {ActionType} تیکت.", ticketModel.Id == 0 ? "ایجاد" : "ویرایش");
            _toastService.ShowError("یک خطای سیستمی رخ داد. لطفاً دوباره تلاش کنید.");
            dispatcher.Dispatch(new SaveTicketFailedAction("• خطایی در ذخیره تیکت رخ داد."));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteTicket(DeleteTicketAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogWarning("درخواست حذف تیکت با شناسه {TicketId}.", action.Id);

            await _ticketService.DeleteAsync(action.Id);
            dispatcher.Dispatch(new LoadTicketsAction());

            _toastService.ShowSuccess($"تیکت '{action.TicketTitle}' با موفقیت حذف شد.");
            _logger.LogInformation("تیکت با شناسه {TicketId} با موفقیت حذف شد.", action.Id);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "تلاش برای حذف تیکتی که وجود ندارد.");
            _toastService.ShowWarning(ex.Message, "یافت نشد");
            dispatcher.Dispatch(new LoadTicketsAction());
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف تیکت با شناسه {TicketId}.", action.Id);
            _toastService.ShowError("خطا در حذف تیکت. لطفاً دوباره تلاش کنید.");
        }
    }

    [EffectMethod]
    public async Task HandleDeleteMultipleTickets(DeleteMultipleTicketsAction action, IDispatcher dispatcher)
    {
        try
        {
            int count = action.Ids.Count();
            _logger.LogWarning("درخواست حذف گروهی تیکت‌ها به تعداد {Count}.", count);

            await _ticketService.DeleteRangeAsync(action.Ids);
            dispatcher.Dispatch(new LoadTicketsAction());

            var successMessage = count == 1 ? "1 تیکت حذف شد." : $"{count} تیکت حذف شدند.";
            _toastService.ShowSuccess(successMessage);
            _logger.LogInformation("تعداد {Count} تیکت با موفقیت حذف شدند.", count);
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
            dispatcher.Dispatch(new LoadTicketsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی تیکت‌ها.");
            _toastService.ShowError("خطا در حذف گروهی تیکت‌ها. لطفاً دوباره تلاش کنید.");
        }
    }
}