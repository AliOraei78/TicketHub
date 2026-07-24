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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many-to-Many: User <-> Role
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many: User <-> Project
            modelBuilder.Entity<UserProject>()
                .HasKey(up => new { up.UserId, up.ProjectId });

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserProjects)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);

            // Ticket relationships
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.ProjectId)
                .IsRequired(false) // <--- این خط به EF Core می‌فهماند که پروژه الزامی نیست
                .OnDelete(DeleteBehavior.SetNull); ;

            // Ticket to Status relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Status)
                .WithMany(s => s.Tickets)
                .HasForeignKey(t => t.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-Many: Transition <-> Role (از طریق TransitionRole)
            modelBuilder.Entity<TransitionRole>()
                .HasKey(tr => new { tr.TransitionId, tr.RoleId });

            modelBuilder.Entity<TransitionRole>()
                .HasOne(tr => tr.Transition)
                .WithMany(t => t.AllowedRoles)
                .HasForeignKey(tr => tr.TransitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransitionRole>()
                .HasOne(tr => tr.Role)
                .WithMany(r => r.TransitionRoles)
                .HasForeignKey(tr => tr.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Ticket <-> TicketHistory
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Ticket)
                .WithMany(t => t.TicketHistories)
                .HasForeignKey(th => th.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Transition <-> TicketHistory
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Transition)
                .WithMany(t => t.Histories)
                .HasForeignKey(th => th.TransitionId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many: Ticket <-> Attachment
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: TicketHistory <-> Attachment
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TicketHistory)
                .WithMany(th => th.Attachments)
                .HasForeignKey(a => a.TicketHistoryId)
                .OnDelete(DeleteBehavior.Restrict); // <--- این خط تغییر کرد

            modelBuilder.Entity<Comment>()
                    .HasOne(c => c.Ticket)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Priority)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Workflow)
                .WithMany(w => w.Projects)
                .HasForeignKey(p => p.WorkflowId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Status>()
                .HasOne(s => s.Workflow)
                .WithMany(w => w.Statuses)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transition>()
                .HasOne(t => t.Workflow)
                .WithMany(w => w.Transitions)
                .HasForeignKey(t => t.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);

            // FromStatus Relationship
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.FromStatus)
                .WithMany()
                .HasForeignKey(th => th.FromStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            // ToStatus Relationship
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ToStatus)
                .WithMany()
                .HasForeignKey(th => th.ToStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transition>()
                .HasOne(t => t.Comment)
                .WithMany()
                .HasForeignKey(t => t.CommentId)
                .OnDelete(DeleteBehavior.SetNull);

            // پیکربندی رابطه TicketHistory و Comment (برای تکمیل ارتباطات موجود در مدل‌های شما)
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Comment)
                .WithMany(c => c.TicketHistories)
                .HasForeignKey(th => th.CommentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Transition)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TransitionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}