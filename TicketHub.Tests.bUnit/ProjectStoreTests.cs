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
using TicketHub.Core.Common.Exceptions;
using TicketHub.Web.Store;
using Xunit;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Tests.bUnit
{
    public class ProjectStoreTests
    {
        [Fact]
        public void ReduceProjectsLoaded_SetsLoadedDataAndStopsLoading()
        {
            // Arrange
            var initialState = new ProjectState(
                true,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                "initial search",
                null
            );

            var projects = new List<ProjectDto> { new ProjectDto { Id = 1, Name = "Alpha" } };
            var workflows = new List<WorkflowDto> { new WorkflowDto { Id = 10, Name = "W1" } };
            var roles = new List<RoleDto> { new RoleDto { Id = 100, Name = "R1" } };

            var action = new ProjectsLoadedAction(projects, workflows, roles);

            // Act
            var newState = ProjectReducers.ReduceProjectsLoaded(initialState, action);

            // Assert
            newState.IsLoading.Should().BeFalse();
            newState.Projects.Should().BeEquivalentTo(projects);
            newState.Workflows.Should().BeEquivalentTo(workflows);
            newState.Roles.Should().BeEquivalentTo(roles);
            newState.SearchTerm.Should().Be("initial search");
        }

        [Fact]
        public void ReduceSetFilter_UpdatesSelectedFilterStatus()
        {
            var initialState = new ProjectState(
                false,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            );

            var action = new SetProjectFilterAction(true);
            var newState = ProjectReducers.ReduceSetFilter(initialState, action);

            newState.SelectedFilterStatus.Should().BeTrue();
        }

        [Fact]
        public void ReduceSetSearch_UpdatesSearchTerm()
        {
            var initialState = new ProjectState(
                false,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            );

            var action = new SetProjectSearchAction("جستجو");
            var newState = ProjectReducers.ReduceSetSearch(initialState, action);

            newState.SearchTerm.Should().Be("جستجو");
        }

        [Fact]
        public async Task HandleLoadProjects_Success_DispatchesProjectsLoadedAction()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var projects = new List<ProjectDto> { new ProjectDto { Id = 1, Name = "Prj1" } };
            var workflows = new List<WorkflowDto> { new WorkflowDto { Id = 2, Name = "Wf1" } };
            var roles = new List<RoleDto> { new RoleDto { Id = 3, Name = "Role1" } };

            mockProjectService.Setup(s => s.GetProjectsAsync()).ReturnsAsync(projects);
            mockProjectService.Setup(s => s.GetWorkflowsAsync()).ReturnsAsync(workflows);
            mockRoleService.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleLoadProjects(new LoadProjectsAction(), mockDispatcher.Object);

            // Assert
            mockDispatcher.Verify(d => d.Dispatch(It.Is<ProjectsLoadedAction>(a =>
                a.Projects == projects && a.Workflows == workflows && a.Roles == roles
            )), Times.Once);
        }

        [Fact]
        public async Task HandleLoadProjects_OnException_ShowsErrorToast()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            mockProjectService.Setup(s => s.GetProjectsAsync()).ThrowsAsync(new Exception("DB error"));

            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleLoadProjects(new LoadProjectsAction(), mockDispatcher.Object);

            // Assert
            mockToastService.Verify(t => t.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task HandleSaveProject_NewProject_CallsAddAndDispatchesLoad()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var newProject = new ProjectDto { Id = 0, Name = "New Project" };
            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleSaveProject(new SaveProjectAction(newProject), mockDispatcher.Object);

            // Assert
            mockProjectService.Verify(s => s.AddProjectAsync(newProject), Times.Once);
            mockToastService.Verify(t => t.ShowSuccess(It.Is<string>(msg => msg.Contains("ایجاد")), It.IsAny<string>()), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }

        [Fact]
        public async Task HandleSaveProject_ExistingProject_CallsUpdateAndDispatchesLoad()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var existingProject = new ProjectDto { Id = 5, Name = "Updated Project" };
            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleSaveProject(new SaveProjectAction(existingProject), mockDispatcher.Object);

            // Assert
            mockProjectService.Verify(s => s.UpdateProjectAsync(existingProject), Times.Once);
            mockToastService.Verify(t => t.ShowSuccess(It.Is<string>(msg => msg.Contains("ذخیره")), It.IsAny<string>()), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }

        [Fact]
        public async Task HandleSaveProject_ValidationException_ShowsWarningToast()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var errors = new Dictionary<string, string[]>
            {
                { "Name", new[] { "نام نامعتبر است" } }
            };
            mockProjectService.Setup(s => s.AddProjectAsync(It.IsAny<ProjectDto>()))
                .ThrowsAsync(new ValidationException(errors));

            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleSaveProject(new SaveProjectAction(new ProjectDto { Id = 0 }), mockDispatcher.Object);

            // Assert
            mockToastService.Verify(t => t.ShowWarning(It.Is<string>(msg => msg.Contains("نام نامعتبر است")), "خطای اطلاعات ورودی"), Times.Once);
        }

        [Fact]
        public async Task HandleDeleteProject_Success_CallsDeleteAndDispatchesLoad()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleDeleteProject(new DeleteProjectAction(15), mockDispatcher.Object);

            // Assert
            mockProjectService.Verify(s => s.DeleteProjectAsync(15), Times.Once);
            mockToastService.Verify(t => t.ShowSuccess(It.Is<string>(msg => msg.Contains("حذف")), It.IsAny<string>()), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }

        [Fact]
        public async Task HandleDeleteProject_NotFoundException_ShowsWarningAndReloads()
        {
            // Arrange
            var mockProjectService = new Mock<IProjectService>();
            var mockRoleService = new Mock<IRoleService>();
            var mockLogger = new Mock<ILogger<ProjectEffects>>();
            var mockToastService = new Mock<IToastService>();
            var mockDispatcher = new Mock<IDispatcher>();

            mockProjectService.Setup(s => s.DeleteProjectAsync(99))
                .ThrowsAsync(new NotFoundException("پروژه", 99));

            var effects = new ProjectEffects(mockProjectService.Object, mockRoleService.Object, mockLogger.Object, mockToastService.Object);

            // Act
            await effects.HandleDeleteProject(new DeleteProjectAction(99), mockDispatcher.Object);

            // Assert
            mockToastService.Verify(t => t.ShowWarning(It.IsAny<string>(), "پروژه یافت نشد"), Times.Once);
            mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }
    }
}
