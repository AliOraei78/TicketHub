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
using TicketHub.Application.Services;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class CategoryStoreTests
    {
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ILogger<CategoryEffects>> _mockLogger;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly CategoryEffects _effects;

        public CategoryStoreTests()
        {
            _mockCategoryService = new Mock<ICategoryService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockLogger = new Mock<ILogger<CategoryEffects>>();
            _mockToastService = new Mock<IToastService>();
            _mockDispatcher = new Mock<IDispatcher>();

            _effects = new CategoryEffects(
                _mockCategoryService.Object,
                _mockProjectService.Object,
                _mockRoleService.Object,
                _mockLogger.Object,
                _mockToastService.Object
            );
        }

        [Fact]
        public void Reducers_ReduceLoadCategories_SetsIsLoadingTrue()
        {
            var initialState = new CategoryState(false, new List<CategoryDto>(), string.Empty, null, new());

            var nextState = CategoryReducers.ReduceLoadCategories(initialState, new LoadCategoriesAction());

            nextState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void Reducers_ReduceCategoriesLoaded_SetsCategoriesAndIsLoadingFalse()
        {
            var initialState = new CategoryState(true, new List<CategoryDto>(), string.Empty, null, new());
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Bug" } };

            var nextState = CategoryReducers.ReduceCategoriesLoaded(initialState, new CategoriesLoadedAction(categories));

            nextState.IsLoading.Should().BeFalse();
            nextState.Categories.Should().HaveCount(1);
        }

        [Fact]
        public void Reducers_ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new CategoryState(false, new List<CategoryDto>(), string.Empty, null, new());

            var nextState = CategoryReducers.ReduceSetSearch(initialState, new SetCategorySearchAction("Bug"));

            nextState.SearchTerm.Should().Be("Bug");
        }

        [Fact]
        public void Reducers_ReduceSetFilterStatus_UpdatesSelectedFilterStatus()
        {
            var initialState = new CategoryState(false, new List<CategoryDto>(), string.Empty, null, new());

            var nextState = CategoryReducers.ReduceSetFilterStatus(initialState, new SetCategoryFilterStatusAction(false));

            nextState.SelectedFilterStatus.Should().BeFalse();
        }

        [Fact]
        public async Task Effects_HandleLoadInitialData_DispatchesCategoriesLoadedAndProjectsLoaded()
        {
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Bug" } };
            var projects = new List<ProjectDto> { new ProjectDto { Id = 1, Name = "Portal" } };
            var workflows = new List<WorkflowDto>();
            var roles = new List<RoleDto>();

            _mockCategoryService.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);
            _mockProjectService.Setup(s => s.GetProjectsAsync()).ReturnsAsync(projects);
            _mockProjectService.Setup(s => s.GetWorkflowsAsync()).ReturnsAsync(workflows);
            _mockRoleService.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            await _effects.HandleLoadInitialData(_mockDispatcher.Object);

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<CategoriesLoadedAction>(a => a.Categories.Count() == 1)), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<ProjectsLoadedAction>(a => a.Projects.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveCategory_CreateMode_CallsAddAndDispatchesLoad()
        {
            var dto = new CategoryDto { Name = "Hardware", IsActive = true };

            await _effects.HandleSaveCategory(new SaveCategoryAction(dto, false), _mockDispatcher.Object);

            _mockCategoryService.Verify(s => s.AddAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadCategoriesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleSaveCategory_EditMode_CallsUpdateAndDispatchesLoad()
        {
            var dto = new CategoryDto { Id = 3, Name = "Software", IsActive = true };

            await _effects.HandleSaveCategory(new SaveCategoryAction(dto, true), _mockDispatcher.Object);

            _mockCategoryService.Verify(s => s.UpdateAsync(dto), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadCategoriesAction>()), Times.Once);
        }

        [Fact]
        public async Task Effects_HandleDeleteCategory_CallsDeleteAndDispatchesLoad()
        {
            await _effects.HandleDeleteCategory(new DeleteCategoryAction(5), _mockDispatcher.Object);

            _mockCategoryService.Verify(s => s.DeleteAsync(It.Is<CategoryDto>(c => c.Id == 5)), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadCategoriesAction>()), Times.Once);
        }
    }
}
