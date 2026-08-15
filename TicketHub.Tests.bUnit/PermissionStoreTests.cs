using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class PermissionStoreTests
    {
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ILogger<PermissionEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly PermissionEffects _effects;

        public PermissionStoreTests()
        {
            _mockPermissionService = new Mock<IPermissionService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockLogger = new Mock<ILogger<PermissionEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new PermissionEffects(
                _mockPermissionService.Object,
                _mockRoleService.Object,
                _mockLogger.Object,
                _mockToastService.Object
            );
        }

        [Fact]
        public void Reducers_ReduceLoadPermissions_SetsIsLoadingTrue()
        {
            var initialState = new PermissionState(false, new List<PermissionDto>(), new List<RoleDto>(), string.Empty, null, new());

            var nextState = PermissionReducers.ReduceLoadPermissions(initialState, new LoadPermissionsAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReducePermissionsLoaded_SetsPermissionsAndIsLoadingFalse()
        {
            var initialState = new PermissionState(true, new List<PermissionDto>(), new List<RoleDto>(), string.Empty, null, new());
            var permissions = new List<PermissionDto> { new PermissionDto { Id = 1, Title = "Perm1" } };

            var nextState = PermissionReducers.ReducePermissionsLoaded(initialState, new PermissionsLoadedAction(permissions));

            nextState.IsLoading.Should().BeFalse();
            nextState.Permissions.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new PermissionState(false, new List<PermissionDto>(), new List<RoleDto>(), string.Empty, null, new());

            var nextState = PermissionReducers.ReduceSetSearch(initialState, new SetPermissionSearchAction("Users.Manage"));

            nextState.SearchTerm.Should().Be("Users.Manage");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new PermissionState(false, new List<PermissionDto>(), new List<RoleDto>(), string.Empty, null, new());

            var nextState = PermissionReducers.ReduceSetFilterStatus(initialState, new SetPermissionFilterStatusAction(true));

            nextState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReduceSetRoleFilter_UpdatesSelectedFilterRoleIds()
        {
            var initialState = new PermissionState(false, new List<PermissionDto>(), new List<RoleDto>(), string.Empty, null, new());

            var nextState = PermissionReducers.ReduceSetRoleFilter(initialState, new SetPermissionRoleFilterAction(new List<int> { 1, 2 }));

            nextState.SelectedFilterRoleIds.Should().Contain(1);
            nextState.SelectedFilterRoleIds.Should().Contain(2);
        }

        [Fact]
        public async Task Effects_HandleLoadPermissions_DispatchesLoadedActions()
        {
            var permissions = new List<PermissionDto> { new PermissionDto { Id = 1, Title = "Perm1" } };
            var roles = new List<RoleDto> { new RoleDto { Id = 1, Name = "Admin" } };

            _mockPermissionService.Setup(s => s.GetAllAsync()).ReturnsAsync(permissions);
            _mockRoleService.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            await _effects.HandleLoadPermissions(new LoadPermissionsAction(), _mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<PermissionsLoadedAction>(a => a.Permissions.Count() == 1)), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<AvailableRolesLoadedAction>(a => a.Roles.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSavePermission_CreateMode_CallsCreateAndDispatchesLoad()
        {
            var dto = new PermissionDto { Title = "PermNew", ResourceKey = "res.new", Type = PermissionType.Full, IsActive = true };

            await _effects.HandleSavePermission(new SavePermissionAction(dto, false), _mockDispatcher.Object);

            _mockPermissionService.Verify(s => s.CreateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPermissionsAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSavePermission_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var dto = new PermissionDto { Id = 3, Title = "PermUpdated", ResourceKey = "res.edit", Type = PermissionType.Full, IsActive = true };

            await _effects.HandleSavePermission(new SavePermissionAction(dto, true), _mockDispatcher.Object);

            _mockPermissionService.Verify(s => s.UpdateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPermissionsAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeletePermission_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeletePermission(new DeletePermissionAction(6), _mockDispatcher.Object);

            _mockPermissionService.Verify(s => s.DeleteAsync(6), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPermissionsAction>()), Times.Once);
        }
    }
}
