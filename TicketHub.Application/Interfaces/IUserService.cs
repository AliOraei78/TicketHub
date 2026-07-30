using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<(List<UserDto> Users, int TotalCount)> GetFilteredUsersAsync(
                string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize);
        Task<UserDto?> GetByIdAsync(int id);
        Task CreateAsync(UserDto dto, string password, List<int> roleIds);
        Task UpdateAsync(UserDto dto, string? password, List<int> roleIds);
        Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null);
        Task RegisterUserAsync(UserDto dto, string plainPassword);
        Task<bool> ConfirmUserAsync(int userId, string token);
    }
}