using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Projects;
using TicketHub.Web.Components.Shared;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class ProjectsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<ProjectState>> _mockState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public ProjectsTests()
        {
            _mockState = new Mock<IState<ProjectState>>();
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            _mockDispatcher = new Mock<IDispatcher>();
            _mockActionSubscriber = new Mock<IActionSubscriber>();

            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockActionSubscriber.Object);
        }

        [Fact]
        public void Projects_RendersCorrectly_WithProjectCards()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto>
                {
                    new ProjectDto { Id = 1, Name = "Project Alpha", Description = "Desc Alpha", IsActive = true },
                    new ProjectDto { Id = 2, Name = "Project Beta", Description = "Desc Beta", IsActive = false }
                },
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            // Act
            var cut = Render<Projects>();

            // Assert
            cut.Markup.Should().Contain("Project Alpha");
            cut.Markup.Should().Contain("Project Beta");
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }

        [Fact]
        public void Projects_LoadingState_ShowsSpinner()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                true,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            // Act
            var cut = Render<Projects>();

            // Assert
            cut.Markup.Should().Contain("در حال بارگذاری اطلاعات پروژه‌ها...");
            cut.FindAll(".animate-spin").Should().NotBeEmpty();
        }

        [Fact]
        public void Projects_EmptyState_ShowsNoProjectsMatched()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto>(),
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            // Act
            var cut = Render<Projects>();

            // Assert
            cut.Markup.Should().Contain("هیچ پروژه‌ای مطابق فیلترهای انتخابی یافت نشد.");
            cut.Markup.Should().Contain("[SYS // NO_PROJECTS_MATCHED]");
        }

        [Fact]
        public void Projects_TelemetryCounts_RenderCalculatedMetrics()
        {
            // Arrange
            var projects = new List<ProjectDto>
            {
                new ProjectDto { Id = 1, Name = "P1", IsActive = true },
                new ProjectDto { Id = 2, Name = "P2", IsActive = true },
                new ProjectDto { Id = 3, Name = "P3", IsActive = false }
            };

            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto { Id = 10, Name = "W1" },
                new WorkflowDto { Id = 20, Name = "W2" }
            };

            var roles = new List<RoleDto>
            {
                new RoleDto { Id = 1, Name = "R1" },
                new RoleDto { Id = 2, Name = "R2" },
                new RoleDto { Id = 3, Name = "R3" },
                new RoleDto { Id = 4, Name = "R4" }
            };

            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                projects,
                workflows,
                roles,
                string.Empty,
                null
            ));

            // Act
            var cut = Render<Projects>();

            // Assert - 5 Telemetry segments
            cut.Markup.Should().Contain("کل پروژه‌ها");
            cut.Markup.Should().Contain("پروژه‌های فعال");
            cut.Markup.Should().Contain("غیرفعال");
            cut.Markup.Should().Contain("جریان‌های کاری");
            cut.Markup.Should().Contain("ماتریس نقش‌ها");
        }

        [Fact]
        public void HandleSearch_DispatchesAction()
        {
            // Arrange
            var cut = Render<Projects>();

            // Act
            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Alpha"));

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetProjectSearchAction>(a => a.Term == "Alpha")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_All_DispatchesSetProjectFilterActionWithNull()
        {
            var cut = Render<Projects>();

            var allButton = cut.Find("button:contains('همه')");
            allButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetProjectFilterAction>(a => a.Status == null)), Times.Once);
        }

        [Fact]
        public void FilterByStatus_Active_DispatchesSetProjectFilterActionWithTrue()
        {
            var cut = Render<Projects>();

            var activeButton = cut.Find("button:contains('فعال')");
            activeButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetProjectFilterAction>(a => a.Status == true)), Times.Once);
        }

        [Fact]
        public void FilterByStatus_Inactive_DispatchesSetProjectFilterActionWithFalse()
        {
            var cut = Render<Projects>();

            var inactiveButton = cut.Find("button:contains('غیرفعال')");
            inactiveButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetProjectFilterAction>(a => a.Status == false)), Times.Once);
        }

        [Fact]
        public void OpenCreateModal_ShowsFormWithEmptyModel()
        {
            // Arrange
            var cut = Render<Projects>();

            // Act
            var createButton = cut.Find("button:contains('ایجاد پروژه جدید')");
            createButton.Click();

            // Assert
            var form = cut.FindComponent<ProjectForm>();
            form.Instance.IsOpen.Should().BeTrue();
            form.Instance.Title.Should().Be("ایجاد پروژه جدید");
            form.Instance.Model.Id.Should().Be(0);
        }

        [Fact]
        public async Task OpenEditModal_ShowsFormWithExistingProject()
        {
            // Arrange
            var project = new ProjectDto { Id = 5, Name = "Project Gamma", Description = "Desc", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto> { project },
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            var cut = Render<Projects>();

            // Act - Trigger edit from card
            var card = cut.FindComponent<ProjectCard>();
            await card.InvokeAsync(() => card.Instance.OnEdit.InvokeAsync());

            // Assert
            var form = cut.FindComponent<ProjectForm>();
            form.Instance.IsOpen.Should().BeTrue();
            form.Instance.Title.Should().Be("ویرایش پروژه");
            form.Instance.Model.Id.Should().Be(5);
            form.Instance.Model.Name.Should().Be("Project Gamma");
        }

        [Fact]
        public async Task SaveProject_DispatchesSaveProjectAction()
        {
            // Arrange
            var cut = Render<Projects>();

            var createButton = cut.Find("button:contains('ایجاد پروژه جدید')");
            createButton.Click();

            var form = cut.FindComponent<ProjectForm>();
            form.Instance.Model.Name = "پروژه تستی نهایی";

            // Act - Trigger OnSave
            await form.InvokeAsync(() => form.Instance.OnSave.InvokeAsync());

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveProjectAction>(a => a.Project.Name == "پروژه تستی نهایی")), Times.Once);
            form.Instance.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task CloseFormModal_SetsIsOpenToFalse()
        {
            // Arrange
            var cut = Render<Projects>();

            var createButton = cut.Find("button:contains('ایجاد پروژه جدید')");
            createButton.Click();

            var form = cut.FindComponent<ProjectForm>();
            form.Instance.IsOpen.Should().BeTrue();

            // Act - Trigger OnCancel
            await form.InvokeAsync(() => form.Instance.OnCancel.InvokeAsync());

            // Assert
            form.Instance.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteProject_ShowsModalAndDispatchesAction()
        {
            // Arrange
            var project = new ProjectDto { Id = 1, Name = "Project Alpha", Description = "Desc", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto> { project },
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));
            var cut = Render<Projects>();

            // Trigger delete from the ProjectCard
            var card = cut.FindComponent<ProjectCard>();
            await card.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync());

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Act - Confirm delete
            await confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            // Assert dispatch
            cut.WaitForAssertion(() => _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteProjectAction>(a => a.Id == 1)), Times.Once), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CancelDelete_ClosesConfirmDeleteModal()
        {
            // Arrange
            var project = new ProjectDto { Id = 1, Name = "Project Alpha", Description = "Desc", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto> { project },
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));
            var cut = Render<Projects>();

            var card = cut.FindComponent<ProjectCard>();
            await card.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync());

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Act - Cancel delete
            await confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            // Assert
            confirmModal.Instance.IsOpen.Should().BeFalse();
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteProjectAction>()), Times.Never);
        }
    }
}
