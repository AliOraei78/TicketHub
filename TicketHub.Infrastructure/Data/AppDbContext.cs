// TicketHub.Infrastructure/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;

namespace TicketHub.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<UserProject> UserProjects { get; set; }
        public DbSet<Role> Roles { get; set; }
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
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }

        // این نسخه نهایی، تمام جداول واسط، کلیدهای ترکیبی و تداخل‌های آبشاری (Cascade Delete) را بدون هیچ خطایی مدیریت می‌کند[cite: 18].
        // کل متد OnModelCreating را با این کد جایگزین کن:

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. تنظیم کلیدهای ترکیبی (Composite Keys)[cite: 11, 12, 13, 15, 16]
            modelBuilder.Entity<UserProject>().HasKey(up => new { up.UserId, up.ProjectId });
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<TransitionRole>().HasKey(tr => new { tr.TransitionId, tr.RoleId });
            modelBuilder.Entity<WorkflowStatus>().HasKey(ws => new { ws.WorkflowId, ws.StatusId });
            modelBuilder.Entity<WorkflowTransition>().HasKey(wt => new { wt.WorkflowId, wt.TransitionId });

            // 2. تنظیم روابط Ticket (جلوگیری از Multiple Cascade Paths)[cite: 3, 10, 14]
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User).WithMany(u => u.Tickets).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project).WithMany(p => p.Tickets).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Status).WithMany(s => s.Tickets).HasForeignKey(t => t.StatusId).OnDelete(DeleteBehavior.Restrict);

            // 3. تنظیم روابط Transition[cite: 2, 5]
            modelBuilder.Entity<Transition>()
                .HasOne(t => t.FromStatus).WithMany(s => s.FromTransitions).HasForeignKey(t => t.FromState).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Transition>()
                .HasOne(t => t.ToStatus).WithMany(s => s.ToTransitions).HasForeignKey(t => t.ToState).OnDelete(DeleteBehavior.Restrict);

            // 4. تنظیم روابط TicketHistory[cite: 2, 4, 14]
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ParentHistory).WithMany(p => p.ChildHistories).HasForeignKey(th => th.ParentHistoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.FromStatus).WithMany(s => s.FromHistories).HasForeignKey(th => th.FromStatusId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ToStatus).WithMany(s => s.ToHistories).HasForeignKey(th => th.ToStatusId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.User).WithMany(u => u.Histories).HasForeignKey(th => th.UserId).OnDelete(DeleteBehavior.Restrict);

            // 5. رفع تداخل Comment و تنظیم دقیق ارتباط 1-به-1[cite: 4, 8, 14]
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.TicketHistory).WithOne(th => th.Comment).HasForeignKey<Comment>(c => c.TicketHistoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Restrict);

            // 6. جلوگیری از تداخل در Attachment[cite: 3, 4, 6]
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Ticket).WithMany(t => t.Attachments).HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TicketHistory).WithMany(th => th.Attachments).HasForeignKey(a => a.TicketHistoryId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}