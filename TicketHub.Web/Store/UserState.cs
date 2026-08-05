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
public record UserState(
    bool IsLoading,
    IEnumerable<UserDto> Users,
    int TotalUsers,
    IEnumerable<RoleDto> AvailableRoles,
    IEnumerable<ProjectDto> AvailableProjects,
    string SearchTerm,
    int PageSize,
    int CurrentPage,
    bool? SelectedFilterStatus,
    List<int> SelectedFilterRoleIds,
    List<int> SelectedFilterProjectIds)
{
    private UserState() : this(true, Array.Empty<UserDto>(), 0, Array.Empty<RoleDto>(), Array.Empty<ProjectDto>(), string.Empty, 10, 1, null, new(), new()) { }
}

// 2. Actions
public record LoadUserInitialDataAction();
public record UserInitialDataLoadedAction(IEnumerable<RoleDto> Roles, IEnumerable<ProjectDto> Projects);
public record LoadUsersAction();
public record UsersLoadedAction(IEnumerable<UserDto> Users, int TotalCount, int ValidatedPage);
public record SetUserFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, bool? Status, List<int>? RoleIds, List<int>? ProjectIds);
public record SaveUserAction(UserDto User, string Password, List<string> SelectedRoles, List<RoleDto> AvailableRoles);
public record SaveUserSuccessAction(); // برای بستن مُدال در کامپوننت نیاز است
public record ExecuteUserBulkAction(HashSet<int> UserIds, string ActionType, int? SingleId = null);

// 3. Reducers
public static class UserReducers
{
    [ReducerMethod]
    public static UserState ReduceLoadUsers(UserState state, LoadUsersAction action) => state with { IsLoading = true };

    [ReducerMethod]
    public static UserState ReduceInitialDataLoaded(UserState state, UserInitialDataLoadedAction action) =>
        state with { AvailableRoles = action.Roles, AvailableProjects = action.Projects };

    [ReducerMethod]
    public static UserState ReduceUsersLoaded(UserState state, UsersLoadedAction action) =>
        state with { IsLoading = false, Users = action.Users, TotalUsers = action.TotalCount, CurrentPage = action.ValidatedPage };

    [ReducerMethod]
    public static UserState ReduceSetFilters(UserState state, SetUserFiltersAction action) =>
        state with
        {
            SearchTerm = action.SearchTerm ?? state.SearchTerm,
            PageSize = action.PageSize ?? state.PageSize,
            CurrentPage = action.CurrentPage ?? state.CurrentPage,
            SelectedFilterStatus = action.Status ?? state.SelectedFilterStatus,
            SelectedFilterRoleIds = action.RoleIds ?? state.SelectedFilterRoleIds,
            SelectedFilterProjectIds = action.ProjectIds ?? state.SelectedFilterProjectIds
        };
}

// 4. Effects
public class UserEffects
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IProjectService _projectService;
    private readonly IState<UserState> _state;
    private readonly ILogger<UserEffects> _logger;
    private readonly IToastService _toastService;

    public UserEffects(
        IUserService userService,
        IRoleService roleService,
        IProjectService projectService,
        IState<UserState> state,
        ILogger<UserEffects> logger,
        IToastService toastService)
    {
        _userService = userService;
        _roleService = roleService;
        _projectService = projectService;
        _state = state;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod(typeof(LoadUserInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع فراخوانی اطلاعات اولیه کاربران.");
            var roles = await _roleService.GetAllRolesAsync();
            var projects = await _projectService.GetProjectsAsync();

            dispatcher.Dispatch(new UserInitialDataLoadedAction(roles, projects));
            dispatcher.Dispatch(new LoadUsersAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات اولیه کاربران.");
            _toastService.ShowError("خطا در بارگذاری اطلاعات اولیه.");
        }
    }

    [EffectMethod(typeof(LoadUsersAction))]
    public async Task HandleLoadUsers(IDispatcher dispatcher)
    {
        try
        {
            var st = _state.Value;
            var result = await _userService.GetFilteredUsersAsync(st.SearchTerm, st.SelectedFilterRoleIds, st.SelectedFilterProjectIds, st.SelectedFilterStatus, st.CurrentPage, st.PageSize);

            int maxPage = result.TotalCount == 0 ? 1 : (int)Math.Ceiling(result.TotalCount / (double)st.PageSize);
            var finalPage = st.CurrentPage;

            if (st.CurrentPage > maxPage && maxPage > 0)
            {
                finalPage = maxPage;
                result = await _userService.GetFilteredUsersAsync(st.SearchTerm, st.SelectedFilterRoleIds, st.SelectedFilterProjectIds, st.SelectedFilterStatus, finalPage, st.PageSize);
            }

            dispatcher.Dispatch(new UsersLoadedAction(result.Users, result.TotalCount, finalPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست کاربران.");
            _toastService.ShowError("خطا در بارگذاری لیست کاربران.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveUser(SaveUserAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("اجرای اکشن SaveUserAction برای {ActionType} کاربر.", action.User.Id == 0 ? "ایجاد" : "ویرایش");

            var roleIdsToAssign = action.AvailableRoles.Where(r => action.SelectedRoles.Contains(r.Name)).Select(r => r.Id).ToList();
            if (action.User.Id == 0)
                await _userService.CreateAsync(action.User, action.Password, roleIdsToAssign);
            else
                await _userService.UpdateAsync(action.User, action.Password, roleIdsToAssign);

            _toastService.ShowSuccess(action.User.Id == 0 ? "کاربر جدید ایجاد شد." : "تغییرات کاربر با موفقیت ذخیره شد.");

            dispatcher.Dispatch(new SaveUserSuccessAction());
            dispatcher.Dispatch(new LoadUsersAction());
        }
        catch (ValidationException ex)
        {
            var errorMessage = string.Join("\n", ex.Errors.SelectMany(e => e.Value));
            _toastService.ShowWarning(errorMessage, "خطای اطلاعات ورودی");
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در زمان {ActionType} کاربر.", action.User.Id == 0 ? "ایجاد" : "ویرایش");
            _toastService.ShowError("خطایی در ذخیره اطلاعات رخ داد.");
        }
    }

    [EffectMethod]
    public async Task HandleBulkAction(ExecuteUserBulkAction action, IDispatcher dispatcher)
    {
        try
        {
            await _userService.ExecuteBulkActionAsync(action.UserIds, action.ActionType, action.SingleId);

            int count = action.SingleId.HasValue ? 1 : (action.UserIds?.Count ?? 0);
            string actionName = (action.ActionType == "Delete" || action.ActionType == "SingleDelete") ? "حذف"
                              : (action.ActionType == "Activate" ? "فعال" : "غیرفعال");

            string verb = count > 1 ? "شدند" : "شد";

            _toastService.ShowSuccess($"{count} کاربر با موفقیت {actionName} {verb}.");

            dispatcher.Dispatch(new LoadUsersAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در انجام عملیات گروهی {ActionType} کاربران.", action.ActionType);
            _toastService.ShowError("امکان انجام عملیات وجود ندارد!");
        }
    }
}