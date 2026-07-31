using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Adapt<List<UserDto>>();
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetFilteredUsersAsync(
        string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize)
    {
        var (users, totalCount) = await _userRepository.GetFilteredUsersAsync(
            searchTerm, roleIds, projectIds, status, page, pageSize);

        return (users.Adapt<List<UserDto>>(), totalCount);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user?.Adapt<UserDto>();
    }

    public async Task CreateAsync(UserDto dto, string password, List<int> roleIds)
    {
        var user = dto.Adapt<User>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);
        await _userRepository.UpdateUserRolesAsync(user.Id, roleIds);
    }

    public async Task UpdateAsync(UserDto dto, string? password, List<int> roleIds)
    {
        var userInDb = await _userRepository.GetByIdAsync(dto.Id);
        if (userInDb == null) return;

        dto.Adapt(userInDb);

        if (!string.IsNullOrWhiteSpace(password))
            userInDb.Password = BCrypt.Net.BCrypt.HashPassword(password);

        await _userRepository.UpdateAsync(userInDb);
        await _userRepository.UpdateUserRolesAsync(userInDb.Id, roleIds);
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

    public async Task RegisterUserAsync(UserDto dto, string plainPassword)
    {
        var user = dto.Adapt<User>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        user.ConfirmationToken = Guid.NewGuid().ToString();
        user.TokenExpiration = DateTime.UtcNow.AddMinutes(15);

        await _userRepository.AddAsync(user);
    }

    public async Task<bool> ConfirmUserAsync(int userId, string token)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.ConfirmationToken != token || user.TokenExpiration < DateTime.UtcNow)
            return false;

        user.IsConfirmed = true;
        user.ConfirmationToken = null;
        user.TokenExpiration = null;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<AuthServiceResponse> LoginAsync(LoginViewModel model)
    {
        var user = await _userRepository.GetByEmailAsync(model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            return new AuthServiceResponse { Success = false, ErrorMessage = "ایمیل یا رمز عبور اشتباه است." };

        if (!user.IsConfirmed)
        {
            string rawCode = new Random().Next(100000, 999999).ToString();
            user.ConfirmationToken = BCrypt.Net.BCrypt.HashPassword(rawCode);
            user.TokenExpiration = DateTime.UtcNow.AddMinutes(2);

            await _userRepository.UpdateAsync(user);

            string emailBody = $@"
        <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
            <h2>کد تایید حساب کاربری</h2>
            <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
            <p>این کد تا ۲ دقیقه معتبر است.</p>
        </div>";

            await _emailService.SendEmailAsync(user.Email, "کد تایید جدید", emailBody);

            return new AuthServiceResponse { Success = false, RequiresConfirmation = true, Email = user.Email };
        }

        return new AuthServiceResponse
        {
            Success = true,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role.Name).ToList()
        };
    }
}