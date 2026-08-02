using Fluxor;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Application.Validations;

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
    List<int> SelectedFilterProjectIds,
    string? FormErrorMessage,
    string? DeleteErrorMessage)
{
    private UserState() : this(true, Array.Empty<UserDto>(), 0, Array.Empty<RoleDto>(), Array.Empty<ProjectDto>(), string.Empty, 10, 1, null, new(), new(), null, null) { }
}

// 2. Actions
public record LoadUserInitialDataAction();
public record UserInitialDataLoadedAction(IEnumerable<RoleDto> Roles, IEnumerable<ProjectDto> Projects);
public record LoadUsersAction();
public record UsersLoadedAction(IEnumerable<UserDto> Users, int TotalCount, int ValidatedPage);
public record SetUserFiltersAction(string? SearchTerm, int? PageSize, int? CurrentPage, bool? Status, List<int>? RoleIds, List<int>? ProjectIds);
public record SaveUserAction(UserDto User, string Password, List<string> SelectedRoles, List<RoleDto> AvailableRoles);
public record SaveUserSuccessAction();
public record SaveUserFailedAction(string ErrorMessage);
public record ExecuteUserBulkAction(HashSet<int> UserIds, string ActionType, int? SingleId = null);
public record SetUserDeleteErrorAction(string ErrorMessage);
public record ClearUserMessagesAction();

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

    [ReducerMethod]
    public static UserState ReduceSaveFailed(UserState state, SaveUserFailedAction action) => state with { FormErrorMessage = action.ErrorMessage };

    [ReducerMethod(typeof(SaveUserSuccessAction))]
    public static UserState ReduceSaveSuccess(UserState state) => state with { FormErrorMessage = null };

    [ReducerMethod]
    public static UserState ReduceDeleteError(UserState state, SetUserDeleteErrorAction action) => state with { DeleteErrorMessage = action.ErrorMessage };

    [ReducerMethod(typeof(ClearUserMessagesAction))]
    public static UserState ReduceClearMessages(UserState state) => state with { FormErrorMessage = null, DeleteErrorMessage = null };
}

// 4. Effects
public class UserEffects
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IProjectService _projectService;
    private readonly IState<UserState> _state;

    public UserEffects(IUserService userService, IRoleService roleService, IProjectService projectService, IState<UserState> state)
    {
        _userService = userService;
        _roleService = roleService;
        _projectService = projectService;
        _state = state;
    }

    [EffectMethod(typeof(LoadUserInitialDataAction))]
    public async Task HandleLoadInitialData(IDispatcher dispatcher)
    {
        var roles = await _roleService.GetAllRolesAsync();
        var projects = await _projectService.GetProjectsAsync();
        dispatcher.Dispatch(new UserInitialDataLoadedAction(roles, projects));
        dispatcher.Dispatch(new LoadUsersAction());
    }

    [EffectMethod(typeof(LoadUsersAction))]
    public async Task HandleLoadUsers(IDispatcher dispatcher)
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

    [EffectMethod]
    public async Task HandleSaveUser(SaveUserAction action, IDispatcher dispatcher)
    {
        var errors = new List<string>();
        var userModel = action.User;

        if (string.IsNullOrWhiteSpace(userModel.Name)) errors.Add("• نام کاربر الزامی است.");

        if (string.IsNullOrWhiteSpace(userModel.Email)) errors.Add("• ایمیل الزامی است.");
        else if (!new ValidEmailAttribute().IsValid(userModel.Email)) errors.Add($"• {new ValidEmailAttribute().ErrorMessage}");
        else
        {
            var allUsers = await _userService.GetAllAsync();
            if (allUsers.Any(u => u.Email == userModel.Email && u.Id != userModel.Id))
                errors.Add("• این ایمیل قبلاً ثبت شده است.");
        }

        if (string.IsNullOrWhiteSpace(userModel.PhoneNumber)) errors.Add("• شماره تلفن الزامی است.");
        else if (!new ValidPhoneNumberAttribute().IsValid(userModel.PhoneNumber)) errors.Add($"• {new ValidPhoneNumberAttribute().ErrorMessage}");

        if (userModel.Id == 0 && string.IsNullOrWhiteSpace(action.Password)) errors.Add("• رمز عبور الزامی است.");
        else if (!string.IsNullOrWhiteSpace(action.Password) && !new StrongPasswordAttribute().IsValid(action.Password))
            errors.Add($"• {new StrongPasswordAttribute().ErrorMessage}");

        if (errors.Any())
        {
            dispatcher.Dispatch(new SaveUserFailedAction(string.Join("\n", errors)));
            return;
        }

        try
        {
            var roleIdsToAssign = action.AvailableRoles.Where(r => action.SelectedRoles.Contains(r.Name)).Select(r => r.Id).ToList();
            if (userModel.Id == 0) await _userService.CreateAsync(userModel, action.Password, roleIdsToAssign);
            else await _userService.UpdateAsync(userModel, action.Password, roleIdsToAssign);

            dispatcher.Dispatch(new SaveUserSuccessAction());
            dispatcher.Dispatch(new LoadUsersAction());
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new SaveUserFailedAction("• خطایی در ذخیره اطلاعات رخ داد."));
        }
    }

    [EffectMethod]
    public async Task HandleBulkAction(ExecuteUserBulkAction action, IDispatcher dispatcher)
    {
        try
        {
            await _userService.ExecuteBulkActionAsync(action.UserIds, action.ActionType, action.SingleId);
            dispatcher.Dispatch(new LoadUsersAction());
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new SetUserDeleteErrorAction("امکان انجام عملیات وجود ندارد!"));
        }
    }
}