using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class RoleStoreTests
    {
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ILogger<RoleEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly RoleEffects _effects;

        public RoleStoreTests()
        {
            _mockRoleService = new Mock<IRoleService>();
            _mockLogger = new Mock<ILogger<RoleEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new RoleEffects(_mockRoleService.Object, _mockLogger.Object, _mockToastService.Object);
        }

        [Fact]
        public void Reducers_ReduceLoadRoles_SetsIsLoadingTrue()
        {
            var initialState = new RoleState(false, new List<RoleDto>(), string.Empty, null);

            var nextState = RoleReducers.ReduceLoadRoles(initialState, new LoadRolesAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReduceRolesLoaded_SetsRolesAndIsLoadingFalse()
        {
            var initialState = new RoleState(true, new List<RoleDto>(), string.Empty, null);
            var roles = new List<RoleDto> { new RoleDto { Id = 1, Name = "Admin" } };

            var nextState = RoleReducers.ReduceRolesLoaded(initialState, new RolesLoadedAction(roles));

            nextState.IsLoading.Should().BeFalse();
            nextState.Roles.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new RoleState(false, new List<RoleDto>(), string.Empty, null);

            var nextState = RoleReducers.ReduceSetSearch(initialState, new SetRoleSearchAction("Support"));

            nextState.SearchTerm.Should().Be("Support");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new RoleState(false, new List<RoleDto>(), string.Empty, null);

            var nextState = RoleReducers.ReduceSetFilterStatus(initialState, new SetRoleFilterStatusAction(true));

            nextState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public async Task Effects_HandleLoadRoles_DispatchesRolesLoadedAction()
        {
            var roles = new List<RoleDto> { new RoleDto { Id = 1, Name = "Admin" } };
            _mockRoleService.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            await _effects.HandleLoadRoles(new LoadRolesAction(), _mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<RolesLoadedAction>(a => a.Roles.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveRole_CreateMode_CallsCreateAndDispatchesLoad()
        {
            var roleDto = new RoleDto { Name = "NewRole", IsActive = true };

            await _effects.HandleSaveRole(new SaveRoleAction(roleDto, false, null), _mockDispatcher.Object);

            _mockRoleService.Verify(s => s.CreateRoleAsync(roleDto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadRolesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveRole_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var roleDto = new RoleDto { Id = 2, Name = "UpdatedRole", IsActive = true };

            await _effects.HandleSaveRole(new SaveRoleAction(roleDto, true, 2), _mockDispatcher.Object);

            _mockRoleService.Verify(s => s.UpdateRoleAsync(2, roleDto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadRolesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeleteRole_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeleteRole(new DeleteRoleAction(7), _mockDispatcher.Object);

            _mockRoleService.Verify(s => s.DeleteRoleAsync(7), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadRolesAction>()), Times.Once);
        }
    }
}
