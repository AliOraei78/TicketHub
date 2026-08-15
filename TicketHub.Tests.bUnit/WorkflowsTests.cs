using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.WorkFlows;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class WorkflowsTests : BUnitComponentTestBase

    {
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IState<WorkflowState>> _mockState;

        public WorkflowsTests()
        {
            _mockDispatcher = new Mock<IDispatcher>();
            _mockState = new Mock<IState<WorkflowState>>();

            var defaultState = new WorkflowState(
                false,
                new List<WorkflowDto>(),
                0,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            );
            _mockState.Setup(s => s.Value).Returns(defaultState);

            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(new Mock<IActionSubscriber>().Object);
            
            // Register NavigationManager manually
            var navMan = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        }

        [Fact]
        public void Initialization_DispatchesLoadInitialDataAction()
        {
            // Act
            var cut = Render<Workflows>();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadWorkflowInitialDataAction>()), Times.Once);
        }

        [Fact]
        public void PageHeader_RendersCorrectly()
        {
            var cut = Render<Workflows>();

            cut.Markup.Should().Contain("جریان‌های کاری");        }

        [Fact]
        public void ClickCreate_NavigatesToEditor()
        {
            // Arrange
            var cut = Render<Workflows>();
            var navMan = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

            // Act
            var createButton = cut.Find("button.bg-gradient-to-r");
            createButton.Click();

            // Assert
            navMan.Uri.Should().Contain("/workflows/editor");        }

        [Fact]
        public void WorkflowCardGrid_RendersWorkflows()
        {
            // Arrange
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto { Id = 1, Name = "Basic Support", Description = "Test Workflow 1" },
                new WorkflowDto { Id = 2, Name = "Bug Tracking", Description = "Test Workflow 2" }
            };
            
            _mockState.Setup(s => s.Value).Returns(new WorkflowState(
                false,
                workflows,
                2,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            ));
            
            // Act
            var cut = Render<Workflows>();
            
            // Assert
            cut.Markup.Should().Contain("Basic Support");            cut.Markup.Should().Contain("Bug Tracking");        }
    }
}
