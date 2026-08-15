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
            cut.Markup.Should().Contain("Admin Permission");            cut.Markup.Should().Contain("admin.access");            
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
            validationMessages.Should().NotBeEmpty();
            cut.Markup.Should().Contain("عنوان دسترسی الزامی است");
            cut.Markup.Should().Contain("کلید منبع"); // Part of "کلید منبع (ResourceKey) الزامی است."
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
            confirmModal.Instance.IsOpen.Should().BeTrue();
            confirmModal.Find("button.bg-rose-600").Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeletePermissionAction>(a => a.Id == 1)), Times.Once);
        }

        [Fact]
        public void TelemetryCards_RenderTotalActiveAndInactiveCounts()
        {
            var permissions = new List<PermissionDto>
            {
                new PermissionDto { Id = 1, Title = "P1", ResourceKey = "res1", IsActive = true },
                new PermissionDto { Id = 2, Title = "P2", ResourceKey = "res2", IsActive = true },
                new PermissionDto { Id = 3, Title = "P3", ResourceKey = "res3", IsActive = false }
            };

            _mockState.Setup(s => s.Value).Returns(new PermissionState(false, permissions, new List<RoleDto>(), string.Empty, null, new()));

            var cut = Render<PermissionsSettings>();

            cut.Markup.Should().Contain("کل دسترسی‌ها");
            cut.Markup.Should().Contain("دسترسی‌های فعال");
            cut.Markup.Should().Contain("دسترسی‌های غیرفعال");
        }

        [Fact]
        public void EditPermission_PopulatesForm_And_DispatchesSaveActionWithIsEditingTrue()
        {
            var permission = new PermissionDto { Id = 1, Title = "OldTitle", ResourceKey = "res.old", Type = PermissionType.Full, IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PermissionState(false, new List<PermissionDto> { permission }, new List<RoleDto>(), string.Empty, null, new()));

            var cut = Render<PermissionsSettings>();

            var editBtn = cut.Find("button[title='ویرایش']");
            editBtn.Click();

            var saveBtn = cut.Find("button[type='submit']");
            saveBtn.TextContent.Should().Contain("ذخیره تغییرات");

            cut.Find("input[placeholder='مثال: مدیریت کاربران']").Change("UpdatedTitle");

            var form = cut.Find("form");
            form.Submit();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SavePermissionAction>(a => a.Permission.Title == "UpdatedTitle" && a.IsEditing == true)), Times.Once);
        }

        [Fact]
        public void DeletePermission_CancelModal_ClosesWithoutDispatchingDelete()
        {
            var permission = new PermissionDto { Id = 1, Title = "Admin", ResourceKey = "admin", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PermissionState(false, new List<PermissionDto> { permission }, new List<RoleDto>(), string.Empty, null, new()));

            var cut = Render<PermissionsSettings>();

            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeletePermissionAction>()), Times.Never);
        }
    }
}

