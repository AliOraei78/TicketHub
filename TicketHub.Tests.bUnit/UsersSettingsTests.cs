using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Users;
using TicketHub.Web.Components.Shared;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class UsersSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<UserState>> _mockUsrState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public UsersSettingsTests()
        {
            _mockUsrState = new Mock<IState<UserState>>();
            _mockUsrState.Setup(s => s.Value).Returns(new UserState(
                false,
                new List<UserDto>(),
                0,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>(),
                0, 0, 0, 0, 0
            ));

            _mockDispatcher = new Mock<IDispatcher>();
            _mockActionSubscriber = new Mock<IActionSubscriber>();

            Services.AddSingleton(_mockUsrState.Object);
            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockActionSubscriber.Object);
        }

        [Fact]
        public void Render_UsersPage_WithHeaderAndTelemetryCards()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = 1, Name = "Admin User", Email = "admin@test.com", IsActive = true },
                new UserDto { Id = 2, Name = "Tech User", Email = "tech@test.com", IsActive = false }
            };

            _mockUsrState.Setup(s => s.Value).Returns(new UserState(
                false,
                users,
                2,
                new List<RoleDto> { new RoleDto { Id = 1, Name = "مدیر" } },
                new List<ProjectDto> { new ProjectDto { Id = 1, Name = "پروژه اصلی" } },
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>(),
                2, 1, 1, 1, 1
            ));

            // Act
            var cut = Render<Users>();

            // Assert
            cut.Markup.Should().Contain("مدیریت کاربران");
            cut.Markup.Should().Contain("کل کاربران");
            cut.Markup.Should().Contain("کاربران فعال");
            cut.Markup.Should().Contain("غیرفعال");
            cut.Markup.Should().Contain("مدیران");
            cut.Markup.Should().Contain("دارای نقش");
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadUserInitialDataAction>()), Times.Once);
        }

        [Fact]
        public void StatusFilters_All_Active_Inactive_DispatchesSetUserFilterStatusAction()
        {
            var cut = Render<Users>();

            var activeButton = cut.Find("button:contains('فعال')");
            activeButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetUserFilterStatusAction>(a => a.Status == true)), Times.Once);

            var inactiveButton = cut.Find("button:contains('غیرفعال')");
            inactiveButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetUserFilterStatusAction>(a => a.Status == false)), Times.Once);

            var allButton = cut.Find("button:contains('همه')");
            allButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetUserFilterStatusAction>(a => a.Status == null)), Times.Once);
        }

        [Fact]
        public void OpenCreateModal_SetsEmptyModelAndOpensModal()
        {
            var cut = Render<Users>();

            var createBtn = cut.Find("button:contains('افزودن کاربر جدید')");
            createBtn.Click();

            var formModal = cut.FindComponent<UserFormModal>();
            formModal.Instance.IsOpen.Should().BeTrue();
            formModal.Instance.Model.Id.Should().Be(0);
        }

        [Fact]
        public void OpenEditModal_SetsExistingUserAndOpensModal()
        {
            var user = new UserDto { Id = 5, Name = "کاربر تستی", Email = "u@test.com", IsActive = true };
            _mockUsrState.Setup(s => s.Value).Returns(new UserState(
                false,
                new List<UserDto> { user },
                1,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Users>();

            var table = cut.FindComponent<UserTable>();
            table.InvokeAsync(() => table.Instance.OnEdit.InvokeAsync(user));

            var formModal = cut.FindComponent<UserFormModal>();
            formModal.Instance.IsOpen.Should().BeTrue();
            formModal.Instance.Model.Id.Should().Be(5);
            formModal.Instance.Model.Name.Should().Be("کاربر تستی");
        }

        [Fact]
        public void SingleDelete_ShowsModalAndDispatchesExecuteUserBulkAction()
        {
            var user = new UserDto { Id = 8, Name = "کاربر برای حذف", Email = "del@test.com", IsActive = true };
            _mockUsrState.Setup(s => s.Value).Returns(new UserState(
                false,
                new List<UserDto> { user },
                1,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Users>();

            var table = cut.FindComponent<UserTable>();
            table.InvokeAsync(() => table.Instance.OnDelete.InvokeAsync(user));

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();
            confirmModal.Instance.Description.Should().Contain("آیا از حذف کاربر کاربر برای حذف مطمئن هستید؟");

            // Confirm
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<ExecuteUserBulkAction>(a => a.ActionType == "SingleDelete" && a.SingleId == 8)), Times.Once);
        }

        [Fact]
        public void CancelDelete_ClosesConfirmDeleteModal()
        {
            var user = new UserDto { Id = 8, Name = "کاربر برای حذف", Email = "del@test.com" };
            _mockUsrState.Setup(s => s.Value).Returns(new UserState(
                false,
                new List<UserDto> { user },
                1,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Users>();

            var table = cut.FindComponent<UserTable>();
            table.InvokeAsync(() => table.Instance.OnDelete.InvokeAsync(user));

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Cancel
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            confirmModal.Instance.IsOpen.Should().BeFalse();
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<ExecuteUserBulkAction>()), Times.Never);
        }
    }
}
