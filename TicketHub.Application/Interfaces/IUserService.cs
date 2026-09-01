using TicketHub.Application.DTOs;
using TicketHub.Application.Models;

namespace TicketHub.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<(List<UserDto> Users, int TotalCount)> GetFilteredUsersAsync(
                string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize);
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto?> GetByEmailAsync(string email);
        Task CreateAsync(UserDto dto, string password, List<int> roleIds);
        Task UpdateAsync(UserDto dto, string? password, List<int> roleIds);
        Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(int userId, string name, string phoneNumber, string? currentPassword, string? newPassword);
        Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null);
        Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(UserDto dto, string plainPassword);
        Task<bool> ConfirmUserAsync(int userId, string token);
        Task<(bool Success, string? Message, int RemainingSeconds)> ResendConfirmationCodeAsync(string email);
        Task<AuthServiceResponse> LoginAsync(LoginViewModel model);
        Task<AuthServiceResponse> ProcessExternalLoginAsync(string provider, string subjectId, string email, string name);
    }
}