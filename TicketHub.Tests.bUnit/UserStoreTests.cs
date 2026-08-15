using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class UserStoreTests
    {
        [Fact]
        public void ReduceLoadUsers_SetsIsLoadingToTrue()
        {
            var initialState = new UserState(
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
                new List<int>()
            );

            var newState = UserReducers.ReduceLoadUsers(initialState, new LoadUsersAction());
            newState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void ReduceInitialDataLoaded_UpdatesMasterCountsAndAvailableData()
        {
            var initialState = new UserState(
                true,
                new List<UserDto>(),
                0,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            );

            var roles = new List<RoleDto> { new RoleDto { Id = 1, Name = "Admin" } };
            var projects = new List<ProjectDto> { new ProjectDto { Id = 2, Name = "Main" } };

            var action = new UserInitialDataLoadedAction(roles, projects, 100, 80, 20, 5, 90);
            var newState = UserReducers.ReduceInitialDataLoaded(initialState, action);

            newState.AvailableRoles.Should().BeEquivalentTo(roles);
            newState.AvailableProjects.Should().BeEquivalentTo(projects);
            newState.MasterTotalUsers.Should().Be(100);
            newState.MasterActiveUsers.Should().Be(80);
            newState.MasterInactiveUsers.Should().Be(20);
            newState.MasterAdminUsers.Should().Be(5);
            newState.MasterAssignedRolesUsers.Should().Be(90);
        }

        [Fact]
        public void ReduceUsersLoaded_SetsUsersAndStopsLoading()
        {
            var initialState = new UserState(
                true,
                new List<UserDto>(),
                0,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            );

            var users = new List<UserDto> { new UserDto { Id = 1, Name = "User1" } };
            var action = new UsersLoadedAction(users, 50, 2);

            var newState = UserReducers.ReduceUsersLoaded(initialState, action);
            newState.IsLoading.Should().BeFalse();
            newState.Users.Should().BeEquivalentTo(users);
            newState.TotalUsers.Should().Be(50);
            newState.CurrentPage.Should().Be(2);
        }

        [Fact]
        public void ReduceSetFilterStatus_UpdatesStatusAndResetsPage()
        {
            var initialState = new UserState(
                false,
                new List<UserDto>(),
                10,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                3,
                null,
                new List<int>(),
                new List<int>()
            );

            var newState = UserReducers.ReduceSetFilterStatus(initialState, new SetUserFilterStatusAction(true));
            newState.SelectedFilterStatus.Should().BeTrue();
            newState.CurrentPage.Should().Be(1);
        }

        [Fact]
        public void ReduceSetFilters_UpdatesProvidedValues()
        {
            var initialState = new UserState(
                false,
                new List<UserDto>(),
                10,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                "old search",
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            );

            var action = new SetUserFiltersAction("new search", 25, 2, null, new List<int> { 1 }, new List<int> { 5 });
            var newState = UserReducers.ReduceSetFilters(initialState, action);

            newState.SearchTerm.Should().Be("new search");
            newState.PageSize.Should().Be(25);
            newState.CurrentPage.Should().Be(2);
            newState.SelectedFilterRoleIds.Should().Contain(1);
            newState.SelectedFilterProjectIds.Should().Contain(5);
        }

        [Fact]
        public async Task HandleLoadUsers_Success_DispatchesUsersLoadedAction()
        {
            var mockUserService = new Mock<IUserService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockProjectService = new Mock<IProjectService>();
            var mockState = new Mock<IState<UserState>>();
            var mockLogger = new Mock<ILogger<UserEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            mockState.Setup(s => s.Value).Returns(new UserState(
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
                new List<int>()
            ));

            var userList = new List<UserDto> { new UserDto { Id = 1, Name = "Ali" } };
            mockUserService.Setup(s => s.GetFilteredUsersAsync(string.Empty, It.IsAny<List<int>>(), It.IsAny<List<int>>(), null, 1, 10))
                .ReturnsAsync((userList, 1));

            var effects = new UserEffects(mockUserService.Object, mockRoleService.Object, mockProjectService.Object, mockState.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleLoadUsers(mockDispatcher.Object);

            // Assert
            mockDispatcher.Verify(d => d.Dispatch(It.Is<UsersLoadedAction>(a => a.TotalCount == 1 && a.ValidatedPage == 1)), Times.Once);
        }

        [Fact]
        public async Task HandleSaveUser_NewUser_DispatchesSuccessAndInitialDataActions()
        {
            var mockUserService = new Mock<IUserService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockProjectService = new Mock<IProjectService>();
            var mockState = new Mock<IState<UserState>>();
            var mockLogger = new Mock<ILogger<UserEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var effects = new UserEffects(mockUserService.Object, mockRoleService.Object, mockProjectService.Object, mockState.Object, mockLogger.Object, mockToastService.Object);

            var newUser = new UserDto { Id = 0, Name = "New User", Email = "new@test.com" };
            var roles = new List<RoleDto> { new RoleDto { Id = 1, Name = "Admin" } };
            var action = new SaveUserAction(newUser, "Pass@1234", new List<string> { "Admin" }, roles);

            // Act
            await effects.HandleSaveUser(action, mockDispatcher.Object);

            // Assert
            mockUserService.Verify(s => s.CreateAsync(newUser, "Pass@1234", It.Is<List<int>>(r => r.Contains(1))), Times.Once);
            mockToastService.Verify(t => t.ShowSuccess("کاربر جدید ایجاد شد.", null), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<SaveUserSuccessAction>()), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadUserInitialDataAction>()), Times.Once);
        }
    }
}
