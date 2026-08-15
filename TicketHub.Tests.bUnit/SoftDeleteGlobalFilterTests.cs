using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Tests.bUnit;

public class SoftDeleteGlobalFilterTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TicketHub_SoftDeleteTest_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SoftDelete_Ticket_ShouldSetIsDeletedAndDeletedAtUtc_InsteadOfPhysicalDelete()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "Test User", Email = "user@test.com", Password = "Password123!" };
        var project = new Project { Name = "Core Project" };
        var status = new Status { Name = "Open" };
        context.Users.Add(user);
        context.Projects.Add(project);
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Bug in production",
            Description = "Critical bug",
            UserId = user.Id,
            ProjectId = project.Id,
            StatusId = status.Id
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act - Remove entity through EF Core
        context.Tickets.Remove(ticket);
        await context.SaveChangesAsync();

        // Assert - Verify in-memory entity state
        ticket.IsDeleted.Should().BeTrue();
        ticket.DeletedAtUtc.Should().NotBeNull();
        ticket.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldExcludeSoftDeletedTickets_FromStandardQueries()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "Test User", Email = "filter@test.com", Password = "Password123!" };
        var project = new Project { Name = "Filter Project" };
        var status = new Status { Name = "Open" };
        context.Users.Add(user);
        context.Projects.Add(project);
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        var activeTicket = new Ticket { Title = "Active Ticket", UserId = user.Id, ProjectId = project.Id, StatusId = status.Id };
        var deletedTicket = new Ticket { Title = "Deleted Ticket", UserId = user.Id, ProjectId = project.Id, StatusId = status.Id };

        context.Tickets.AddRange(activeTicket, deletedTicket);
        await context.SaveChangesAsync();

        context.Tickets.Remove(deletedTicket);
        await context.SaveChangesAsync();

        // Act
        var visibleTickets = await context.Tickets.ToListAsync();

        // Assert
        visibleTickets.Should().HaveCount(1);
        visibleTickets.First().Title.Should().Be("Active Ticket");
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnSoftDeletedTickets()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "Test User", Email = "ignore@test.com", Password = "Password123!" };
        var project = new Project { Name = "Project" };
        var status = new Status { Name = "Open" };
        context.Users.Add(user);
        context.Projects.Add(project);
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        var ticket = new Ticket { Title = "Ignored Filter Ticket", UserId = user.Id, ProjectId = project.Id, StatusId = status.Id };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        context.Tickets.Remove(ticket);
        await context.SaveChangesAsync();

        // Act
        var allTicketsIncludingDeleted = await context.Tickets.IgnoreQueryFilters().ToListAsync();

        // Assert
        allTicketsIncludingDeleted.Should().HaveCount(1);
        allTicketsIncludingDeleted.First().IsDeleted.Should().BeTrue();
        allTicketsIncludingDeleted.First().DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDelete_ProjectAndUserAndComment_ShouldExcludeFromStandardQueries()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "Deletable User", Email = "deluser@test.com", Password = "Password123!" };
        var project = new Project { Name = "Deletable Project" };
        var category = new Category { Name = "Deletable Category" };
        var priority = new Priority { Name = "Deletable Priority" };
        var status = new Status { Name = "Deletable Status" };
        var workflow = new Workflow { Name = "Deletable Workflow" };
        var role = new Role { Name = "Deletable Role" };
        var ticketField = new TicketField { Name = "Deletable Field" };
        var permission = new Permission { Title = "Deletable Permission" };
        var notification = new Notification { UserId = 1, Title = "Deletable Notif", Message = "Msg" };

        context.Users.Add(user);
        context.Projects.Add(project);
        context.Categories.Add(category);
        context.Priorities.Add(priority);
        context.Statuses.Add(status);
        context.Workflows.Add(workflow);
        context.Roles.Add(role);
        context.TicketFields.Add(ticketField);
        context.Permissions.Add(permission);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act - Soft Delete all of them
        context.Users.Remove(user);
        context.Projects.Remove(project);
        context.Categories.Remove(category);
        context.Priorities.Remove(priority);
        context.Statuses.Remove(status);
        context.Workflows.Remove(workflow);
        context.Roles.Remove(role);
        context.TicketFields.Remove(ticketField);
        context.Permissions.Remove(permission);
        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();

        // Assert - Normal queries return empty
        (await context.Users.AnyAsync()).Should().BeFalse();
        (await context.Projects.AnyAsync()).Should().BeFalse();
        (await context.Categories.AnyAsync()).Should().BeFalse();
        (await context.Priorities.AnyAsync()).Should().BeFalse();
        (await context.Statuses.AnyAsync()).Should().BeFalse();
        (await context.Workflows.AnyAsync()).Should().BeFalse();
        (await context.Roles.AnyAsync()).Should().BeFalse();
        (await context.TicketFields.AnyAsync()).Should().BeFalse();
        (await context.Permissions.AnyAsync()).Should().BeFalse();
        (await context.Notifications.AnyAsync()).Should().BeFalse();

        // Assert - IgnoreQueryFilters returns all items with IsDeleted == true
        (await context.Users.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Projects.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Categories.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Priorities.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Statuses.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Workflows.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Roles.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.TicketFields.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Permissions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.Notifications.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }
}
