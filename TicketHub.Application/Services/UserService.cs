using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
}