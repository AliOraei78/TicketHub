using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Permissions;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Web.Components.Shared;
using System.Collections.Generic;
using System;
using TicketHub.Application.Enums;

namespace TicketHub.Tests.bUnit
{
    public class PermissionsSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<PermissionState>> _mockState;
        private readonly Mock<IState<RoleState>> _mockRoleState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public PermissionsSettingsTests()
        {
            _mockState = new Mock<IState<PermissionState>>();
            _mockRoleState = new Mock<IState<RoleState>>();

            _mockState.Setup(s => s.Value).Returns(new PermissionState(
                false,
                new List<PermissionDto>(),
                new List<RoleDto>(),
                string.Empty,
                null,
                new List<int>()
            ));

            _mockRoleState.Setup(s => s.Value).Returns(new RoleState(
                false,
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            _mockDispatcher = new Mock<IDispatcher>();
            _mockActionSubscriber = new Mock<IActionSubscriber>();

            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(_mockRoleState.Object);
            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockActionSubscriber.Object);
        }

        [Fact]
        public void PermissionsSettings_RendersCorrectly()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new PermissionState(
                false,
                new List<PermissionDto>
                {
                    new PermissionDto { Id = 1, Title = "Admin Permission", ResourceKey = "admin.access", Type = PermissionType.Full, IsActive = true, RoleIds = new List<int>() }
                },
                new List<RoleDto>(),
                string.Empty,
                null,
                new List<int>()
            ));

            // Act
            var cut = Render<PermissionsSettings>();

            // Assert
            Assert.Contains("Admin Permission", cut.Markup);
            Assert.Contains("admin.access", cut.Markup);
            
            // Check if LoadPermissionsAction was dispatched
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPermissionsAction>()), Times.Once);
        }

        [Fact]
        public void SubmitEmptyForm_ShowsValidationMessages()
        {
            // Arrange
            var cut = Render<PermissionsSettings>();

            // Act - Submit empty form
            var form = cut.FindComponent<PermissionForm>();
            form.Find("form").Submit();

            // Assert
            var validationMessages = cut.FindAll(".validation-message, .text-red-500");
            Assert.NotEmpty(validationMessages);
            Assert.Contains("عنوان دسترسی الزامی است", cut.Markup);
            Assert.Contains("کلید منبع", cut.Markup); // Part of "کلید منبع (ResourceKey) الزامی است."
        }

        [Fact]
        public void HandleSearch_DispatchesAction()
        {
            // Arrange
            var cut = Render<PermissionsSettings>();

            // Act
            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Admin"));

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetPermissionSearchAction>(a => a.Term == "Admin")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_DispatchesAction()
        {
            // Arrange
            var cut = Render<PermissionsSettings>();

            // Act
            var activeButton = cut.Find("button:contains('فعال')");
            activeButton.Click();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetPermissionFilterStatusAction>(a => a.Status == true)), Times.Once);
        }

        [Fact]
        public void DeletePermission_ShowsModalAndDispatchesAction()
        {
            // Arrange
            var permission = new PermissionDto { Id = 1, Title = "Admin Permission", ResourceKey = "admin", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PermissionState(
                false,
                new List<PermissionDto> { permission },
                new List<RoleDto>(),
                string.Empty,
                null,
                new List<int>()
            ));
            var cut = Render<PermissionsSettings>();

            // Trigger delete from the DataGrid row
            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            Assert.True(confirmModal.Instance.IsOpen);

            // Act - Confirm delete
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            // Assert dispatch
            cut.WaitForAssertion(() => _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeletePermissionAction>(a => a.Id == 1)), Times.Once), TimeSpan.FromSeconds(2));
        }
    }
}
