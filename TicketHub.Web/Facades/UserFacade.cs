using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Web.States;
using Mapster;

namespace TicketHub.Web.Facades;

public class UserFacade
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Project> _projectRepository;

    public UserFacade(IUserRepository userRepository, IRepository<Role> roleRepo, IRepository<Project> projectRepo)
    {
        _userRepository = userRepository;
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
        var result = await _userRepository.GetFilteredUsersAsync(
            state.SearchTerm,
            state.SelectedFilterRoleIds,
            state.SelectedFilterProjectIds,
            state.SelectedFilterStatus,
            state.CurrentPage,
            state.PageSize);

        var userDtos = result.Users.Adapt<List<UserDto>>();
        return (userDtos, result.TotalCount);
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
            var allUsers = await _userRepository.GetAllAsync();
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
            var newUser = userModel.Adapt<User>();
            newUser.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);
            newUser.CreatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.UpdateUserRolesAsync(newUser.Id, roleIdsToAssign);
        }
        else
        {
            var userInDb = await _userRepository.GetByIdAsync(userModel.Id);
            if (userInDb == null) return errors;

            userInDb.Name = userModel.Name;
            userInDb.Email = userModel.Email;
            userInDb.PhoneNumber = userModel.PhoneNumber;
            userInDb.IsConfirmed = userModel.IsConfirmed;
            userInDb.IsActive = userModel.IsActive;
            if (!string.IsNullOrWhiteSpace(passwordInput))
                userInDb.Password = BCrypt.Net.BCrypt.HashPassword(passwordInput);

            await _userRepository.UpdateAsync(userInDb);
            await _userRepository.UpdateUserRolesAsync(userInDb.Id, roleIdsToAssign);
        }

        return errors;
    }

    public async Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null)
    {
        switch (actionType)
        {
            case "Delete":
                await _userRepository.BulkDeleteAsync(userIds); break;
            case "SingleDelete":
                if (singleId.HasValue) await _userRepository.DeleteAsync(singleId.Value); break;
            case "Activate":
                await _userRepository.BulkUpdateStatusAsync(userIds, true); break;
            case "Deactivate":
                await _userRepository.BulkUpdateStatusAsync(userIds, false); break;
        }
    }
}
