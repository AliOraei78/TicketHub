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
    public class PriorityStoreTests
    {
        private readonly Mock<IPriorityService> _mockPriorityService;
        private readonly Mock<ILogger<PriorityEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly PriorityEffects _effects;

        public PriorityStoreTests()
        {
            _mockPriorityService = new Mock<IPriorityService>();
            _mockLogger = new Mock<ILogger<PriorityEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new PriorityEffects(_mockPriorityService.Object, _mockLogger.Object, _mockToastService.Object);
        }

        [Fact]
        public void Reducers_ReduceLoadPriorities_SetsIsLoadingTrue()
        {
            var initialState = new PriorityState(false, new List<PriorityDto>(), string.Empty, null);

            var nextState = PriorityReducers.ReduceLoadPriorities(initialState, new LoadPrioritiesAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReducePrioritiesLoaded_SetsPrioritiesAndIsLoadingFalse()
        {
            var initialState = new PriorityState(true, new List<PriorityDto>(), string.Empty, null);
            var priorities = new List<PriorityDto> { new PriorityDto { Id = 1, Name = "High" } };

            var nextState = PriorityReducers.ReducePrioritiesLoaded(initialState, new PrioritiesLoadedAction(priorities));

            nextState.IsLoading.Should().BeFalse();
            nextState.Priorities.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new PriorityState(false, new List<PriorityDto>(), string.Empty, null);

            var nextState = PriorityReducers.ReduceSetSearch(initialState, new SetPrioritySearchAction("Critical"));

            nextState.SearchTerm.Should().Be("Critical");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new PriorityState(false, new List<PriorityDto>(), string.Empty, null);

            var nextState = PriorityReducers.ReduceSetFilterStatus(initialState, new SetPriorityFilterStatusAction(true));

            nextState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public async Task Effects_HandleLoadPriorities_DispatchesPrioritiesLoadedAction()
        {
            var priorities = new List<PriorityDto> { new PriorityDto { Id = 1, Name = "High" } };
            _mockPriorityService.Setup(s => s.GetAllAsync()).ReturnsAsync(priorities);

            await _effects.HandleLoadPriorities(new LoadPrioritiesAction(), _mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<PrioritiesLoadedAction>(a => a.Priorities.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSavePriority_CreateMode_CallsAddAndDispatchesLoad()
        {
            var dto = new PriorityDto { Name = "Urgent", Level = 10, IsActive = true };

            await _effects.HandleSavePriority(new SavePriorityAction(dto, false), _mockDispatcher.Object);

            _mockPriorityService.Verify(s => s.AddAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPrioritiesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSavePriority_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var dto = new PriorityDto { Id = 5, Name = "UpdatedPriority", Level = 15, IsActive = true };

            await _effects.HandleSavePriority(new SavePriorityAction(dto, true), _mockDispatcher.Object);

            _mockPriorityService.Verify(s => s.UpdateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPrioritiesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeletePriority_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeletePriority(new DeletePriorityAction(8), _mockDispatcher.Object);

            _mockPriorityService.Verify(s => s.DeleteAsync(8), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPrioritiesAction>()), Times.Once);
        }
    }
}
