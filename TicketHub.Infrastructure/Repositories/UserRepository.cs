// TicketHub.Infrastructure/Repositories/UserRepository.cs
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<(List<User> Users, int TotalCount)> GetFilteredUsersAsync(string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize)
        {
            var query = _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RoleProjects).ThenInclude(rp => rp.Project)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));

            if (roleIds.Any())
                query = query.Where(u => u.UserRoles.Any(ur => roleIds.Contains(ur.RoleId)));

            if (projectIds.Any())
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleProjects.Any(rp => projectIds.Contains(rp.ProjectId))));

            if (status.HasValue)
                query = query.Where(u => u.IsActive == status.Value);

            var total = await query.CountAsync();
            var users = await query.OrderBy(u => u.Id)
                        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (users, total);
        }

        public async Task UpdateUserRolesAsync(int userId, List<int> roleIds)
        {
            var currentRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _context.UserRoles.RemoveRange(currentRoles);

            var newRoles = roleIds.Select(roleId => new UserRole { UserId = userId, RoleId = roleId });
            await _context.UserRoles.AddRangeAsync(newRoles);
            await SaveChangesAsync();
        }

        public async Task BulkUpdateStatusAsync(HashSet<int> userIds, bool isActive)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            users.ForEach(u => u.IsActive = isActive);
            await SaveChangesAsync();
        }

        public async Task BulkDeleteAsync(HashSet<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}