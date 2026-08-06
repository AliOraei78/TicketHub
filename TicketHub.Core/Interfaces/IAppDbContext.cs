using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;

namespace TicketHub.Core.Interfaces;

public interface IAppDbContext
{
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Project> Projects { get; set; }
    DbSet<Ticket> Tickets { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<RoleProject> RoleProjects { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<Status> Statuses { get; set; }
    DbSet<Transition> Transitions { get; set; }
    DbSet<TicketHistory> TicketHistories { get; set; }
    DbSet<TransitionRole> TransitionRoles { get; set; }
    DbSet<Attachment> Attachments { get; set; }
    DbSet<Comment> Comments { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Priority> Priorities { get; set; }
    DbSet<Workflow> Workflows { get; set; }
    DbSet<WorkflowStatus> WorkflowStatuses { get; set; }
    DbSet<FieldType> FieldTypes { get; set; }
    DbSet<TransitionField> TransitionFields { get; set; }
    DbSet<CategoryProject> CategoryProjects { get; set; }
    DbSet<CategoryRole> CategoryRoles { get; set; }
    DbSet<TicketField> TicketFields { get; set; }
    DbSet<FieldCategory> FieldCategories { get; set; }
    DbSet<TicketFieldValue> TicketFieldValues { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}