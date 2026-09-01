using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class FullTextSearchTests
    {
        [Fact]
        public void Ticket_SearchVector_CanBeInstantiatedAndAssigned()
        {
            // Arrange
#pragma warning disable CS0618
            var vector = NpgsqlTsVector.Parse("network:1 connection:2 timeout:3");
#pragma warning restore CS0618
            var ticket = new Ticket
            {
                Id = 1,
                Title = "Network connection timeout error",
                Description = "VPN gateway is unreachable from branch office",
                SearchVector = vector
            };

            // Assert
            Assert.NotNull(ticket.SearchVector);
            Assert.Equal("Network connection timeout error", ticket.Title);
            Assert.Equal("VPN gateway is unreachable from branch office", ticket.Description);
        }

        [Fact]
        public void AppDbContext_TicketConfiguration_ValidatesModelStructure()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TicketHub_Fts_TestDb_" + Guid.NewGuid())
                .Options;

            using var context = new AppDbContext(options);
            var model = context.Model;

            // Act
            var ticketEntity = model.FindEntityType(typeof(Ticket));

            // Assert
            Assert.NotNull(ticketEntity);

            // Verify RowVersion Concurrency check is present
            var rowVersionProp = ticketEntity.FindProperty(nameof(Ticket.RowVersion));
            Assert.NotNull(rowVersionProp);
            Assert.True(rowVersionProp.IsConcurrencyToken);

            // Verify primary foreign keys exist
            Assert.NotNull(ticketEntity.FindProperty(nameof(Ticket.UserId)));
            Assert.NotNull(ticketEntity.FindProperty(nameof(Ticket.ProjectId)));
            Assert.NotNull(ticketEntity.FindProperty(nameof(Ticket.StatusId)));
        }

        [Fact]
        public void Ticket_InitialState_HasNonNullRowVersionAndEmptyCollections()
        {
            // Arrange
            var ticket = new Ticket
            {
                Title = "High CPU load on server",
                Description = "Database worker thread consuming 99% CPU"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, ticket.RowVersion);
            Assert.NotNull(ticket.Attachments);
            Assert.NotNull(ticket.TicketHistories);
            Assert.NotNull(ticket.Comments);
            Assert.NotNull(ticket.FieldValues);
        }
    }
}
