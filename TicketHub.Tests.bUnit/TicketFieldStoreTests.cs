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
    public class TicketFieldStoreTests
    {
        private readonly Mock<ITicketFieldService> _mockTicketFieldService;
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<IFieldTypeService> _mockFieldTypeService;
        private readonly Mock<ILogger<TicketFieldEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly TicketFieldEffects _effects;

        public TicketFieldStoreTests()
        {
            _mockTicketFieldService = new Mock<ITicketFieldService>();
            _mockCategoryService = new Mock<ICategoryService>();
            _mockFieldTypeService = new Mock<IFieldTypeService>();
            _mockLogger = new Mock<ILogger<TicketFieldEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new TicketFieldEffects(
                _mockTicketFieldService.Object,
                _mockCategoryService.Object,
                _mockFieldTypeService.Object,
                _mockLogger.Object,
                _mockToastService.Object
            );
        }

        [Fact]
        public void Reducers_ReduceLoadTicketFields_SetsIsLoadingTrue()
        {
            var initialState = new TicketFieldState(false, new List<TicketFieldDto>(), string.Empty, null, new(), new());

            var nextState = TicketFieldReducers.ReduceLoadTicketFields(initialState, new LoadTicketFieldsAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReduceTicketFieldsLoaded_SetsFieldsAndIsLoadingFalse()
        {
            var initialState = new TicketFieldState(true, new List<TicketFieldDto>(), string.Empty, null, new(), new());
            var fields = new List<TicketFieldDto> { new TicketFieldDto { Id = 1, Name = "Field1" } };

            var nextState = TicketFieldReducers.ReduceTicketFieldsLoaded(initialState, new TicketFieldsLoadedAction(fields));

            nextState.IsLoading.Should().BeFalse();
            nextState.TicketFields.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new TicketFieldState(false, new List<TicketFieldDto>(), string.Empty, null, new(), new());

            var nextState = TicketFieldReducers.ReduceSetSearch(initialState, new SetTicketFieldSearchAction("Serial"));

            nextState.SearchTerm.Should().Be("Serial");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new TicketFieldState(false, new List<TicketFieldDto>(), string.Empty, null, new(), new());

            var nextState = TicketFieldReducers.ReduceSetFilterStatus(initialState, new SetTicketFieldFilterStatusAction(true));

            nextState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public async Task Effects_HandleLoadInitialData_DispatchesLoadedActions()
        {
            var fields = new List<TicketFieldDto> { new TicketFieldDto { Id = 1, Name = "F1" } };
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "C1" } };
            var fieldTypes = new List<FieldTypeDto> { new FieldTypeDto { Id = 1, Type = "Text" } };

            _mockTicketFieldService.Setup(s => s.GetAllAsync()).ReturnsAsync(fields);
            _mockCategoryService.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);
            _mockFieldTypeService.Setup(s => s.GetAllAsync()).ReturnsAsync(fieldTypes);

            await _effects.HandleLoadInitialData(_mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<TicketFieldsLoadedAction>(a => a.TicketFields.Count() == 1)), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<TicketFieldDependenciesLoadedAction>(a => a.Categories.Count() == 1 && a.FieldTypes.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveTicketField_CreateMode_CallsAddAndDispatchesLoad()
        {
            var dto = new TicketFieldDto { Name = "Phone", IsActive = true };

            await _effects.HandleSaveTicketField(new SaveTicketFieldAction(dto, false), _mockDispatcher.Object);

            _mockTicketFieldService.Verify(s => s.AddAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadTicketFieldsAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveTicketField_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var dto = new TicketFieldDto { Id = 2, Name = "UpdatedPhone", IsActive = true };

            await _effects.HandleSaveTicketField(new SaveTicketFieldAction(dto, true), _mockDispatcher.Object);

            _mockTicketFieldService.Verify(s => s.UpdateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadTicketFieldsAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeleteTicketField_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeleteTicketField(new DeleteTicketFieldAction(4), _mockDispatcher.Object);

            _mockTicketFieldService.Verify(s => s.DeleteAsync(It.Is<TicketFieldDto>(f => f.Id == 4)), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadTicketFieldsAction>()), Times.Once);
        }
    }
}
