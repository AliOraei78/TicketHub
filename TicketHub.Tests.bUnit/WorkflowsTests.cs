using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.WorkFlows;
using TicketHub.Web.Components.Shared;
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
        }

        [Fact]
        public void Initialization_DispatchesLoadInitialDataAction()
        {
            var cut = Render<Workflows>();
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadWorkflowInitialDataAction>()), Times.Once);
        }

        [Fact]
        public void PageHeader_And_Telemetry_RendersCorrectly()
        {
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto
                {
                    Id = 1,
                    Name = "Basic Support",
                    Description = "Default workflow",
                    WorkflowStatuses = new List<WorkflowStatusDto> { new(), new() },
                    Transitions = new List<TransitionDto> { new() },
                    Projects = new List<ProjectDto> { new() }
                }
            };

            _mockState.Setup(s => s.Value).Returns(new WorkflowState(
                false,
                workflows,
                1,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Workflows>();

            cut.Markup.Should().Contain("جریان‌های کاری");
            cut.Markup.Should().Contain("کل جریان‌ها");
            cut.Markup.Should().Contain("کل گام‌ها");
            cut.Markup.Should().Contain("کل انتقال‌ها");
            cut.Markup.Should().Contain("پروژه‌های متصل");
            cut.Markup.Should().Contain("جریان‌های فعال");
        }

        [Fact]
        public void ClickCreate_NavigatesToEditor()
        {
            var cut = Render<Workflows>();
            var navMan = Services.GetRequiredService<NavigationManager>();

            var createButton = cut.Find("button.btn-cyber-primary");
            createButton.Click();

            navMan.Uri.Should().Contain("/workflows/editor");
        }

        [Fact]
        public void SingleDelete_ShowsConfirmModal_And_DispatchesDeleteWorkflowAction()
        {
            var workflow = new WorkflowDto { Id = 7, Name = "جریان تست حذف" };
            _mockState.Setup(s => s.Value).Returns(new WorkflowState(
                false,
                new List<WorkflowDto> { workflow },
                1,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Workflows>();

            var grid = cut.FindComponent<WorkflowCardGrid>();
            grid.InvokeAsync(() => grid.Instance.OnDelete.InvokeAsync(workflow));

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();
            confirmModal.Instance.Description.Should().Contain("آیا از حذف جریان کاری 'جریان تست حذف' اطمینان دارید؟");

            // Confirm
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnConfirm.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteWorkflowAction>(a => a.Id == 7 && a.WorkflowName == "جریان تست حذف")), Times.Once);
        }

        [Fact]
        public void CancelDelete_ClosesModalWithoutDispatchingAction()
        {
            var workflow = new WorkflowDto { Id = 7, Name = "جریان تست حذف" };
            _mockState.Setup(s => s.Value).Returns(new WorkflowState(
                false,
                new List<WorkflowDto> { workflow },
                1,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Workflows>();

            var grid = cut.FindComponent<WorkflowCardGrid>();
            grid.InvokeAsync(() => grid.Instance.OnDelete.InvokeAsync(workflow));

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Cancel
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            confirmModal.Instance.IsOpen.Should().BeFalse();
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteWorkflowAction>()), Times.Never);
        }
    }
}
