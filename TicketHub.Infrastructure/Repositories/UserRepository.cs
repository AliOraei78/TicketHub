// TicketHub.Infrastructure/Repositories/UserRepository.cs
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public async Task<(List<User> Users, int TotalCount)> GetFilteredUsersAsync(string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize)
        {
            using var context = await _factory.CreateDbContextAsync();

            var query = context.Users
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
            using var context = await _factory.CreateDbContextAsync();
            var currentRoles = await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            context.UserRoles.RemoveRange(currentRoles);

            var newRoles = roleIds.Select(roleId => new UserRole { UserId = userId, RoleId = roleId });
            await context.UserRoles.AddRangeAsync(newRoles);

            // ذخیره‌سازی محلی کانتکست
            await context.SaveChangesAsync();
        }

        public async Task BulkUpdateStatusAsync(HashSet<int> userIds, bool isActive)
        {
            using var context = await _factory.CreateDbContextAsync();
            var users = await context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            users.ForEach(u => u.IsActive = isActive);
            await context.SaveChangesAsync();
        }

        public async Task BulkDeleteAsync(HashSet<int> userIds)
        {
            using var context = await _factory.CreateDbContextAsync();
            var users = await context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            context.Users.RemoveRange(users);
            await context.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}