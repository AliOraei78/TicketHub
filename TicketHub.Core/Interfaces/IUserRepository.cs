using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<(List<User> Users, int TotalCount)> GetFilteredUsersAsync(string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize);
    Task UpdateUserRolesAsync(int userId, List<int> roleIds);
    Task BulkUpdateStatusAsync(HashSet<int> userIds, bool isActive);
    Task BulkDeleteAsync(HashSet<int> userIds);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByExternalProviderAsync(string provider, string subjectId);
}