// TicketHub.Infrastructure/Data/AppDbContext.cs
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

using MassTransit;

namespace TicketHub.Infrastructure.Data
{
    public class AppDbContext : AuditDbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleProject> RoleProjects { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Transition> Transitions { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        public DbSet<TransitionRole> TransitionRoles { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Priority> Priorities { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<WorkflowStatus> WorkflowStatuses { get; set; }
        public DbSet<FieldType> FieldTypes { get; set; }
        public DbSet<TransitionField> TransitionFields { get; set; }
        public DbSet<CategoryProject> CategoryProjects { get; set; }
        public DbSet<TicketField> TicketFields { get; set; }
        public DbSet<FieldCategory> FieldCategories { get; set; }
        public DbSet<TicketFieldValue> TicketFieldValues { get; set; }
        public DbSet<TransitionFieldValue> TransitionFieldValues { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(w => w
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddTransactionalOutboxEntities();

            modelBuilder.Entity<UserRole>().HasKey(ur => ur.Id);
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            // استفاده از فیلد Id به عنوان کلید اصلی برای این دو جدول به جای کلید ترکیبی
            modelBuilder.Entity<TransitionRole>().HasKey(tr => tr.Id);
            modelBuilder.Entity<WorkflowStatus>()
            .HasKey(ws => ws.Id); // تبدیل Id به کلید اصلی

            modelBuilder.Entity<WorkflowStatus>()
                .HasIndex(ws => ws.NodeId)
                .IsUnique(); // شناسه روی بوم باید یکتا باشد

            // 2. تنظیم روابط Ticket (جلوگیری از Multiple Cascade Paths)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User).WithMany(u => u.Tickets).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project).WithMany(p => p.Tickets).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Status).WithMany(s => s.Tickets).HasForeignKey(t => t.StatusId).OnDelete(DeleteBehavior.Restrict);

            // 3. تنظیم روابط Transition
            modelBuilder.Entity<Transition>()
                .HasOne(t => t.FromStatus).WithMany(ws => ws.FromTransitions).HasForeignKey(t => t.FromState).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Transition>()
                .HasOne(t => t.ToStatus).WithMany(ws => ws.ToTransitions).HasForeignKey(t => t.ToState).OnDelete(DeleteBehavior.Restrict);

            // 4. تنظیم روابط TicketHistory برای رفع خطای Multiple Cascade Paths در SQL Server
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Ticket).WithMany(t => t.TicketHistories).HasForeignKey(th => th.TicketId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Transition).WithMany(t => t.Histories).HasForeignKey(th => th.TransitionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.User).WithMany(u => u.Histories).HasForeignKey(th => th.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Role).WithMany(r => r.Histories).HasForeignKey(th => th.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ParentHistory).WithMany(p => p.ChildHistories).HasForeignKey(th => th.ParentHistoryId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.WorkFlow).WithMany(w => w.TicketHistories).HasForeignKey(th => th.WorkFlowId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.FromStatus).WithMany(s => s.FromHistories).HasForeignKey(th => th.FromStatusId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ToStatus).WithMany(s => s.ToHistories).HasForeignKey(th => th.ToStatusId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Restrict);

            // 7. جلوگیری از تداخل در Attachment
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Ticket).WithMany(t => t.Attachments).HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TicketHistory).WithMany(th => th.Attachments).HasForeignKey(a => a.TicketHistoryId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transition>()
                .HasOne(t => t.Workflow)
                .WithMany(w => w.Transitions)
                .HasForeignKey(t => t.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowStatus>()
                .HasOne(ws => ws.Workflow)
                .WithMany(w => w.WorkflowStatuses)
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // به AppDbContext.cs (بخش 7) اضافه شود
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TransitionField)
                .WithMany(tf => tf.Attachments)
                .HasForeignKey(a => a.TransitionFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. تنظیم روابط TransitionField
            modelBuilder.Entity<TransitionField>()
                .HasOne(tf => tf.Transition)
                .WithMany(t => t.TransitionFields)
                .HasForeignKey(tf => tf.TransitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransitionField>()
                .HasOne(tf => tf.FieldType)
                .WithMany(ft => ft.TransitionFields)
                .HasForeignKey(tf => tf.FieldTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // تنظیمات9.  RoleProject
            modelBuilder.Entity<RoleProject>().HasKey(rp => rp.Id);
            modelBuilder.Entity<RoleProject>()
                .HasIndex(rp => new { rp.RoleId, rp.ProjectId })
                .IsUnique();

            // تنظیمات CategoryProject
            modelBuilder.Entity<CategoryProject>().HasKey(cp => cp.Id);
            modelBuilder.Entity<CategoryProject>()
                .HasIndex(cp => new { cp.CategoryId, cp.ProjectId })
                .IsUnique();

            modelBuilder.Entity<CategoryProject>()
                .HasOne(cp => cp.Category)
                .WithMany(c => c.CategoryProjects)
                .HasForeignKey(cp => cp.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategoryProject>()
                .HasOne(cp => cp.Project)
                .WithMany(p => p.CategoryProjects)
                .HasForeignKey(cp => cp.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // تنظیمات FieldCategory
            modelBuilder.Entity<FieldCategory>().HasKey(cr => cr.Id);
            modelBuilder.Entity<FieldCategory>()
                        .HasIndex(cp => new { cp.CategoryId, cp.TicketFieldId })
                        .IsUnique();

            modelBuilder.Entity<FieldCategory>()
                        .HasOne(cr => cr.Category)
                        .WithMany(c => c.FieldCategories)
                        .HasForeignKey(cr => cr.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FieldCategory>()
                .HasOne(cr => cr.TicketField)
                .WithMany(r => r.FieldCategories)
                .HasForeignKey(cr => cr.TicketFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketField>()
                        .HasOne(t => t.FieldType)
                        .WithMany(w => w.TicketFields)
                        .HasForeignKey(t => t.FieldTypeId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketFieldValue>().HasKey(tfv => tfv.Id);

            // جلوگیری از ثبت دو مقدار برای یک فیلد در یک تیکت واحد
            modelBuilder.Entity<TicketFieldValue>()
                .HasIndex(tfv => new { tfv.TicketId, tfv.TicketFieldId })
                .IsUnique();

            modelBuilder.Entity<TicketFieldValue>()
                .HasOne(tfv => tfv.Ticket)
                .WithMany(t => t.FieldValues)
                .HasForeignKey(tfv => tfv.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketFieldValue>()
                .HasOne(tfv => tfv.TicketField)
                .WithMany(tf => tf.TicketFieldValues)
                .HasForeignKey(tfv => tfv.TicketFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TicketFieldValue)
                .WithMany(tfv => tfv.Attachments)
                .HasForeignKey(a => a.TicketFieldValueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransitionFieldValue>().HasKey(tfv => tfv.Id);

            modelBuilder.Entity<TransitionFieldValue>()
                .HasIndex(tfv => new { tfv.TicketHistoryId, tfv.TransitionFieldId })
                .IsUnique();

            modelBuilder.Entity<TransitionFieldValue>()
                .HasOne(tfv => tfv.TicketHistory)
                .WithMany(th => th.TransitionFieldValues)
                .HasForeignKey(tfv => tfv.TicketHistoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransitionFieldValue>()
                .HasOne(tfv => tfv.TransitionField)
                .WithMany(tf => tf.TransitionFieldValues)
                .HasForeignKey(tfv => tfv.TransitionFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TransitionFieldValue)
                .WithMany(tfv => tfv.Attachments)
                .HasForeignKey(a => a.TransitionFieldValueId)
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط جدید بین Ticket و WorkflowStatus
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.WorkflowStatus)
                .WithMany(ws => ws.Tickets)
                .HasForeignKey(t => t.WorkflowStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>().HasKey(rp => rp.Id);
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>().HasKey(n => n.Id);
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Global Query Filter & Index for ISoftDeletable entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                    var falseConstant = System.Linq.Expressions.Expression.Constant(false);
                    var body = System.Linq.Expressions.Expression.Equal(property, falseConstant);
                    var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);
                    entityType.SetQueryFilter(lambda);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasIndex(nameof(ISoftDeletable.IsDeleted));
                }
            }
        }

        public override int SaveChanges()
        {
            ApplySoftDelete();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplySoftDelete();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplySoftDelete();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplySoftDelete();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplySoftDelete()
        {
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                }
            }
        }
    }
}