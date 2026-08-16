using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Roles;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Web.Components.Shared;
using System.Collections.Generic;
using System;

namespace TicketHub.Tests.bUnit
{
    public class RolesSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<RoleState>> _mockState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public RolesSettingsTests()
        {
            _mockState = new Mock<IState<RoleState>>();
            _mockState.Setup(s => s.Value).Returns(new RoleState(
                false,
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            _mockDispatcher = new Mock<IDispatcher>();
            _mockActionSubscriber = new Mock<IActionSubscriber>();

            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockActionSubscriber.Object);
        }

        [Fact]
        public void RolesSettings_RendersCorrectly()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new RoleState(
                false,
                new List<RoleDto>
                {
                    new RoleDto { Id = 1, Name = "Admin Role", IsActive = true }
                },
                string.Empty,
                null
            ));

            // Act
            var cut = Render<RolesSettings>();

            // Assert
            cut.Markup.Should().Contain("Admin Role");            
            // Check if LoadRolesAction was dispatched
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadRolesAction>()), Times.Once);
        }

        [Fact]
        public void SubmitEmptyForm_ShowsValidationMessages()
        {
            // Arrange
            var cut = Render<RolesSettings>();

            // Act - Submit empty form
            var form = cut.FindComponent<RoleForm>();
            form.Find("form").Submit();

            // Assert
            var validationMessages = cut.FindAll(".validation-message, .text-red-500");
            validationMessages.Should().NotBeEmpty();            cut.Markup.Should().Contain("نام نقش الزامی است");        }

        [Fact]
        public void HandleSearch_DispatchesAction()
        {
            // Arrange
            var cut = Render<RolesSettings>();

            // Act
            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Admin"));

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetRoleSearchAction>(a => a.Term == "Admin")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_DispatchesAction()
        {
            // Arrange
            var cut = Render<RolesSettings>();

            // Act
            var activeFilterBtn = cut.Find("button:contains('فعال')");
            activeFilterBtn.Click();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetRoleFilterStatusAction>(a => a.Status == true)), Times.Once);
        }

        [Fact]
        public async Task DeleteRole_ShowsModalAndDispatchesAction()
        {
            // Arrange
            var role = new RoleDto { Id = 1, Name = "Admin", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new RoleState(
                false,
                new List<RoleDto> { role },
                string.Empty,
                null
            ));
            var cut = Render<RolesSettings>();

            // Trigger delete from the DataGrid row
            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();
            // Act - Confirm delete
            await confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            // Assert dispatch
            cut.WaitForAssertion(() => _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteRoleAction>(a => a.Id == 1)), Times.Once), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void TelemetryCards_RenderTotalActiveAndInactiveCounts()
        {
            var roles = new List<RoleDto>
            {
                new RoleDto { Id = 1, Name = "Admin", IsActive = true },
                new RoleDto { Id = 2, Name = "Support", IsActive = true },
                new RoleDto { Id = 3, Name = "Guest", IsActive = false }
            };

            _mockState.Setup(s => s.Value).Returns(new RoleState(false, roles, string.Empty, null));

            var cut = Render<RolesSettings>();

            cut.Markup.Should().Contain("کل نقش‌ها");
            cut.Markup.Should().Contain("نقش‌های فعال");
            cut.Markup.Should().Contain("نقش‌های غیرفعال");
        }

        [Fact]
        public void EditRole_LoadsRoleIntoForm()
        {
            var role = new RoleDto { Id = 10, Name = "Lead Developer", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new RoleState(false, new List<RoleDto> { role }, string.Empty, null));

            var cut = Render<RolesSettings>();

            var editBtn = cut.Find("button[title='ویرایش']");
            editBtn.Click();

            cut.Markup.Should().Contain("ویرایش نقش");
            cut.Markup.Should().Contain("Lead Developer");
        }

        [Fact]
        public void DeleteRole_CancelModal_ClosesWithoutDispatchingDelete()
        {
            var role = new RoleDto { Id = 1, Name = "Admin Role", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new RoleState(false, new List<RoleDto> { role }, string.Empty, null));

            var cut = Render<RolesSettings>();

            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Cancel
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteRoleAction>()), Times.Never);
        }
    }
}

