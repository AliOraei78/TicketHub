using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Components.Pages.Main.Tickets;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketCardTests : BUnitComponentTestBase
    {
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

        public TicketCardTests()
        {
            _mockPermissionService = new Mock<IPermissionService>();
            _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

            Services.AddSingleton(_mockPermissionService.Object);
            Services.AddSingleton(_mockAuthStateProvider.Object);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "مدیر سیستم"),
                new Claim(ClaimTypes.Role, "ادمین")
            }, "TestAuth"));
            _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync())
                .ReturnsAsync(new AuthenticationState(userClaims));

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);
        }

        [Fact]
        public void Render_TicketCard_DisplaysTitle_Project_Status_AndPriority()
        {
            var ticket = new TicketDto
            {
                Id = 15,
                Title = "باگ لاگین در نسخه وب",
                Description = "شرح خطای لاگین با پیام رمز عبور اشتباه",
                Project = new ProjectDto { Name = "پورتال مرکزی" },
                User = new UserDto { Name = "علی رضایی" },
                Status = new StatusDto { Name = "در حال بررسی", ColorCode = "#84cc16" },
                Priority = new PriorityDto { Name = "بحرانی", Level = 4, ColorCode = "#ef4444" },
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(2)
            };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
                .Add(p => p.CanDelete, true)
                .Add(p => p.CanSelect, true)
            );

            cut.Markup.Should().Contain("#15");            cut.Markup.Should().Contain("باگ لاگین در نسخه وب");            cut.Markup.Should().Contain("پورتال مرکزی");            cut.Markup.Should().Contain("علی رضایی");            cut.Markup.Should().Contain("در حال بررسی");            cut.Markup.Should().Contain("بحرانی");            cut.Markup.Should().Contain("EPIC");            cut.Markup.Should().Contain("مانده");        }

        [Fact]
        public void Render_OverdueTicket_DisplaysOverdueBadge_AndPulseAlert()
        {
            var ticket = new TicketDto
            {
                Id = 16,
                Title = "تیکت منقضی شده",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddHours(-5),
                Priority = new PriorityDto { Level = 2, Name = "متوسط" }
            };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
            );

            cut.Markup.Should().Contain("OVERDUE");            cut.Markup.Should().Contain("گذشته");        }

        [Fact]
        public void Click_Card_InvokesOnClickCallback()
        {
            int clickedId = 0;
            var ticket = new TicketDto { Id = 88, Title = "تیکت کلیکی", CreatedAt = DateTime.UtcNow };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
                .Add(p => p.OnClick, (int id) => { clickedId = id; })
            );

            cut.Find(".cyber-quest-capsule").Click();

            clickedId.Should().Be(88);        }

        [Fact]
        public void Click_Action_InvokesOnActionClickCallback()
        {
            TicketDto? clickedTicket = null;
            var ticket = new TicketDto { Id = 77, Title = "تیکت عملیات", CreatedAt = DateTime.UtcNow };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
                .Add(p => p.OnActionClick, (TicketDto t) => { clickedTicket = t; })
            );

            var actionBtn = cut.Find("button[title='عملیات و تغییر وضعیت']");
            actionBtn.Click();

            clickedTicket.Should().NotBeNull();            clickedTicket.Id.Should().Be(77);        }

        [Fact]
        public void Click_Delete_InvokesOnDeleteClickCallback()
        {
            TicketDto? deletedTicket = null;
            var ticket = new TicketDto { Id = 99, Title = "تیکت برای حذف", CreatedAt = DateTime.UtcNow };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
                .Add(p => p.CanDelete, true)
                .Add(p => p.OnDeleteClick, (TicketDto t) => { deletedTicket = t; })
            );

            var deleteBtn = cut.Find("button[title='حذف تیکت']");
            deleteBtn.Click();

            deletedTicket.Should().NotBeNull();            deletedTicket.Id.Should().Be(99);        }

        [Fact]
        public void Toggle_Checkbox_InvokesIsSelectedChanged()
        {
            bool selectionValue = false;
            var ticket = new TicketDto { Id = 50, Title = "تیکت چک‌باکس", CreatedAt = DateTime.UtcNow };

            var cut = Render<TicketCard>(parameters => parameters
                .Add(p => p.Ticket, ticket)
                .Add(p => p.CanSelect, true)
                .Add(p => p.IsSelected, false)
                .Add(p => p.IsSelectedChanged, (bool val) => { selectionValue = val; })
            );

            var checkbox = cut.Find("input[type='checkbox']");
            checkbox.Change(true);

            selectionValue.Should().BeTrue();        }
    }
}
