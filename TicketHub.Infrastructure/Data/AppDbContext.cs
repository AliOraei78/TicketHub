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
        public DbSet<TransitionHistory> TransitionHistories { get; set; }
        public DbSet<TransitionRole> TransitionRoles { get; set; }
        public DbSet<Attachment> Attachments { get; set; }

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

            // One-to-Many: Ticket <-> TransitionHistory
            modelBuilder.Entity<TransitionHistory>()
                .HasOne(th => th.Ticket)
                .WithMany(t => t.TransitionHistories)
                .HasForeignKey(th => th.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Transition <-> TransitionHistory
            modelBuilder.Entity<TransitionHistory>()
                .HasOne(th => th.Transition)
                .WithMany(t => t.Histories)
                .HasForeignKey(th => th.TransitionId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Ticket <-> Attachment
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: TransitionHistory <-> Attachment
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.TransitionHistory)
                .WithMany(th => th.Attachments)
                .HasForeignKey(a => a.TransitionHistoryId)
                .OnDelete(DeleteBehavior.Restrict); // <--- این خط تغییر کرد
        }
    }
}