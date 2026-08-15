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
    public class StatusStoreTests
    {
        private readonly Mock<IStatusService> _mockStatusService;
        private readonly Mock<ILogger<StatusEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly StatusEffects _effects;

        public StatusStoreTests()
        {
            _mockStatusService = new Mock<IStatusService>();
            _mockLogger = new Mock<ILogger<StatusEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new StatusEffects(_mockStatusService.Object, _mockLogger.Object, _mockToastService.Object);
        }

        [Fact]
        public void Reducers_ReduceLoadStatuses_SetsIsLoadingTrue()
        {
            var initialState = new StatusState(false, new List<StatusDto>(), string.Empty, null);

            var nextState = StatusReducers.ReduceLoadStatuses(initialState, new LoadStatusesAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReduceStatusesLoaded_SetsStatusesAndIsLoadingFalse()
        {
            var initialState = new StatusState(true, new List<StatusDto>(), string.Empty, null);
            var statuses = new List<StatusDto> { new StatusDto { Id = 1, Name = "Open" } };

            var nextState = StatusReducers.ReduceStatusesLoaded(initialState, new StatusesLoadedAction(statuses));

            nextState.IsLoading.Should().BeFalse();
            nextState.Statuses.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new StatusState(false, new List<StatusDto>(), string.Empty, null);

            var nextState = StatusReducers.ReduceSetSearch(initialState, new SetStatusSearchAction("Closed"));

            nextState.SearchTerm.Should().Be("Closed");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new StatusState(false, new List<StatusDto>(), string.Empty, null);

            var nextState = StatusReducers.ReduceSetFilterStatus(initialState, new SetStatusFilterStatusAction(true));

            nextState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public async Task Effects_HandleLoadStatuses_DispatchesStatusesLoadedAction()
        {
            var statuses = new List<StatusDto> { new StatusDto { Id = 1, Name = "Open" } };
            _mockStatusService.Setup(s => s.GetAllAsync()).ReturnsAsync(statuses);

            await _effects.HandleLoadStatuses(new LoadStatusesAction(), _mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<StatusesLoadedAction>(a => a.Statuses.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveStatus_CreateMode_CallsAddAndDispatchesLoad()
        {
            var dto = new StatusDto { Name = "NewStatus", IsActive = true };

            await _effects.HandleSaveStatus(new SaveStatusAction(dto, false), _mockDispatcher.Object);

            _mockStatusService.Verify(s => s.AddAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadStatusesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveStatus_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var dto = new StatusDto { Id = 2, Name = "UpdatedStatus", IsActive = true };

            await _effects.HandleSaveStatus(new SaveStatusAction(dto, true), _mockDispatcher.Object);

            _mockStatusService.Verify(s => s.UpdateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadStatusesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeleteStatus_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeleteStatus(new DeleteStatusAction(4), _mockDispatcher.Object);

            _mockStatusService.Verify(s => s.DeleteAsync(4), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadStatusesAction>()), Times.Once);
        }
    }
}
