using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class UserFacade
{
    private readonly IUserService _userService;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Project> _projectRepository;

    public UserFacade(IUserService userService, IRepository<Role> roleRepo, IRepository<Project> projectRepo)
    {
        _userService = userService;
        _roleRepository = roleRepo;
        _projectRepository = projectRepo;
    }

    // منطق متدهای GetInitialDataAsync و GetUsersAsync (شامل Mapster) را اینجا قرار دهید...

    public async Task<(List<RoleDto> Roles, List<ProjectDto> Projects)> GetInitialDataAsync()
    {
        var roles = (await _roleRepository.GetAllAsync()).Adapt<List<RoleDto>>();
        var projects = (await _projectRepository.GetAllAsync()).Adapt<List<ProjectDto>>();
        return (roles, projects);
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetUsersAsync(UserState state)
    {
        return await _userService.GetFilteredUsersAsync(
            state.SearchTerm,
            state.SelectedFilterRoleIds,
            state.SelectedFilterProjectIds,
            state.SelectedFilterStatus,
            state.CurrentPage,
            state.PageSize);
    }

    public async Task<List<string>> SaveUserAsync(UserDto userModel, string passwordInput, List<string> selectedRoles, List<RoleDto> availableRoles)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(userModel.Name)) errors.Add("• نام کاربر الزامی است.");

        if (string.IsNullOrWhiteSpace(userModel.Email)) errors.Add("• ایمیل الزامی است.");
        else if (!new ValidEmailAttribute().IsValid(userModel.Email))
            errors.Add($"• {new ValidEmailAttribute().ErrorMessage}");
        else
        {
            var allUsers = await _userService.GetAllAsync();
            if (allUsers.Any(u => u.Email == userModel.Email && u.Id != userModel.Id))
                errors.Add("• این ایمیل قبلاً ثبت شده است.");
        }

        if (string.IsNullOrWhiteSpace(userModel.PhoneNumber)) errors.Add("• شماره تلفن الزامی است.");
        else if (!new ValidPhoneNumberAttribute().IsValid(userModel.PhoneNumber))
            errors.Add($"• {new ValidPhoneNumberAttribute().ErrorMessage}");

        if (userModel.Id == 0 && string.IsNullOrWhiteSpace(passwordInput))
            errors.Add("• رمز عبور الزامی است.");
        else if (!string.IsNullOrWhiteSpace(passwordInput) && !new StrongPasswordAttribute().IsValid(passwordInput))
            errors.Add($"• {new StrongPasswordAttribute().ErrorMessage}");

        if (errors.Any()) return errors;

        var roleIdsToAssign = availableRoles.Where(r => selectedRoles.Contains(r.Name)).Select(r => r.Id).ToList();
        // این بلوک را جایگزین کنید
        if (userModel.Id == 0)
        {
            await _userService.CreateAsync(userModel, passwordInput, roleIdsToAssign);
        }
        else
        {
            await _userService.UpdateAsync(userModel, passwordInput, roleIdsToAssign);
        }

        return errors;
    }

    // 4. تغییر متد ExecuteBulkActionAsync
    public async Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null)
    {
        await _userService.ExecuteBulkActionAsync(userIds, actionType, singleId);
    }
}
