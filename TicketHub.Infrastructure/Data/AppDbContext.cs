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

        // این نسخه نهایی، تمام جداول واسط، کلیدهای ترکیبی و تداخل‌های آبشاری (Cascade Delete) را بدون هیچ خطایی مدیریت می‌کند[cite: 18].
        // کل متد OnModelCreating را با این کد جایگزین کن:

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .HasOne(t => t.FromStatus).WithMany(s => s.FromTransitions).HasForeignKey(t => t.FromState).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Transition>()
                .HasOne(t => t.ToStatus).WithMany(s => s.ToTransitions).HasForeignKey(t => t.ToState).OnDelete(DeleteBehavior.Restrict);

            // 4. تنظیم روابط TicketHistory برای رفع خطای Multiple Cascade Paths در SQL Server
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Ticket).WithMany(t => t.TicketHistories).HasForeignKey(th => th.TicketId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Transition).WithMany(t => t.Histories).HasForeignKey(th => th.TransitionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.User).WithMany(u => u.Histories).HasForeignKey(th => th.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Role).WithMany(u => u.Histories).HasForeignKey(th => th.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ParentHistory).WithMany(p => p.ChildHistories).HasForeignKey(th => th.ParentHistoryId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.WorkFlow).WithMany(w => w.TicketHistories).HasForeignKey(th => th.WorkFlowId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.FromStatus).WithMany(s => s.FromHistories).HasForeignKey(th => th.FromStatusId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ToStatus).WithMany(s => s.ToHistories).HasForeignKey(th => th.ToStatusId).OnDelete(DeleteBehavior.Restrict);

            // 5. تنظیم رابطه Comment در تاریخچه
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Comment).WithMany().HasForeignKey(th => th.CommentId).OnDelete(DeleteBehavior.Restrict);

            // 6. رفع تداخل Comment و تنظیم دقیق ارتباط 1-به-1
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.TicketHistory).WithOne(th => th.Comment).HasForeignKey<Comment>(c => c.TicketHistoryId).OnDelete(DeleteBehavior.Restrict);
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
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}