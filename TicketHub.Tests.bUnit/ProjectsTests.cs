using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Projects;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Web.Components.Shared;

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
        public void Projects_RendersCorrectly()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new ProjectState(
                false,
                new List<ProjectDto>
                {
                    new ProjectDto { Id = 1, Name = "Project Alpha", Description = "Desc", IsActive = true }
                },
                new List<WorkflowDto>(),
                new List<RoleDto>(),
                string.Empty,
                null
            ));

            // Act
            var cut = Render<Projects>();

            // Assert
            Assert.Contains("Project Alpha", cut.Markup);
            
            // Check if LoadProjectsAction was dispatched
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadProjectsAction>()), Times.Once);
        }

        [Fact]
        public void SubmitEmptyForm_ShowsValidationMessages()
        {
            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new ProjectDto())
            );

            // Act - Submit empty form
            cut.Find("form").Submit();

            // Assert
            var validationMessages = cut.FindAll(".validation-message, .text-red-500");
            Assert.NotEmpty(validationMessages);
        }

        [Fact]
        public void OpenCreateModal_ShowsForm()
        {
            // Arrange
            var cut = Render<Projects>();

            // Act
            var createButton = cut.Find("button:contains('ایجاد پروژه جدید')");
            createButton.Click();

            // Assert
            var form = cut.FindComponent<ProjectForm>();
            Assert.True(form.Instance.IsOpen);
            Assert.Equal("ایجاد پروژه جدید", form.Instance.Title);
            Assert.Equal(0, form.Instance.Model.Id);
        }

        [Fact]
        public void HandleSearch_DispatchesAction()
        {
            // Arrange
            var cut = Render<Projects>();

            // Act
            // Since SearchBox is a component, we can invoke its OnSearchChanged callback
            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Alpha"));

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetProjectSearchAction>(a => a.Term == "Alpha")), Times.Once);
        }

        [Fact]
        public void DeleteProject_ShowsModalAndDispatchesAction()
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

            // Trigger delete from the ProjectCard (mocking the event)
            var card = cut.FindComponent<ProjectCard>();
            card.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync());

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            Assert.True(confirmModal.Instance.IsOpen);

            // Act - Confirm delete
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            // Since it has Task.Delay, we might need to await the dispatch in test or verify it
            // Using Task.Delay in the component means we should wait in the test too
            // Just verifying it eventually dispatches
            // In bUnit we can use WaitForAssertion, but since ConfirmDelete is async we can do:
            cut.WaitForAssertion(() => _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteProjectAction>(a => a.Id == 1)), Times.Once), TimeSpan.FromSeconds(2));
        }
    }
}
