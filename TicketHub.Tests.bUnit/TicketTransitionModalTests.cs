using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Components.Pages.Main.Tickets;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketTransitionModalTests : BUnitComponentTestBase
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IValidator<ExecuteTransitionDto>> _mockValidator;
        private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

        private readonly WorkflowDto _sampleWorkflow;

        public TicketTransitionModalTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockToastService = new Mock<IToastService>();
            _mockValidator = new Mock<IValidator<ExecuteTransitionDto>>();
            _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

            Services.AddSingleton(_mockTicketService.Object);
            Services.AddSingleton(_mockWorkflowService.Object);
            Services.AddSingleton(_mockRoleService.Object);
            Services.AddSingleton(_mockToastService.Object);
            Services.AddSingleton(_mockValidator.Object);
            Services.AddSingleton(_mockAuthStateProvider.Object);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "ادمین"),
                new Claim(ClaimTypes.Role, "ادمین")
            }, "TestAuth"));
            _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync())
                .ReturnsAsync(new AuthenticationState(userClaims));

            _sampleWorkflow = new WorkflowDto
            {
                Id = 1,
                Name = "جریان کاری عمومی",
                WorkflowStatuses = new List<WorkflowStatusDto>
                {
                    new() { Id = 10, StatusId = 1, Status = new StatusDto { Id = 1, Name = "جدید" } },
                    new() { Id = 20, StatusId = 2, Status = new StatusDto { Id = 2, Name = "در حال بررسی" } },
                    new() { Id = 30, StatusId = 3, Status = new StatusDto { Id = 3, Name = "خاتمه یافته" } }
                },
                Transitions = new List<TransitionDto>
                {
                    new()
                    {
                        Id = 101,
                        Name = "شروع بررسی تیکت",
                        FromState = 10,
                        ToState = 20,
                        IsActive = true,
                        ToStatus = new WorkflowStatusDto { Id = 20, StatusId = 2, Status = new StatusDto { Name = "در حال بررسی" } },
                        TransitionFields = new List<TransitionFieldDto>
                        {
                            new() { Id = 1, FieldName = "علت بررسی", FieldTypeId = 1, IsRequired = true, IsActive = true, SortOrder = 1 }
                        }
                    },
                    new()
                    {
                        Id = 102,
                        Name = "بستن سریع",
                        FromState = 10,
                        ToState = 30,
                        IsActive = true,
                        ToStatus = new WorkflowStatusDto { Id = 30, StatusId = 3, Status = new StatusDto { Name = "خاتمه یافته" } }
                    }
                }
            };

            _mockWorkflowService.Setup(w => w.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(_sampleWorkflow);

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ExecuteTransitionDto>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        [Fact]
        public async Task OpenAsync_RendersAvailableTransitions_ForCurrentStatus()
        {
            var cut = Render<TicketTransitionModal>();

            await cut.InvokeAsync(() => cut.Instance.OpenAsync(
                ticketId: 42,
                title: "تیکت تستی برای تغییر وضعیت",
                currentStatusId: 1,
                workflowId: 1,
                currentWorkflowStatusId: 10
            ));

            cut.Instance.IsVisible.Should().BeTrue();            ModalMarkup.Should().Contain("ثبت عملیات: تیکت تستی برای تغییر وضعیت");            ModalMarkup.Should().Contain("شروع بررسی تیکت");            ModalMarkup.Should().Contain("بستن سریع");            ModalMarkup.Should().Contain("در حال بررسی");        }

        [Fact]
        public async Task SelectTransition_DisplaysTransitionFields_AndCommentBox()
        {
            var cut = Render<TicketTransitionModal>();

            await cut.InvokeAsync(() => cut.Instance.OpenAsync(
                ticketId: 42,
                title: "تیکت تستی",
                currentStatusId: 1,
                workflowId: 1,
                currentWorkflowStatusId: 10
            ));

            // Click the first transition (TransitionId = 101)
            var transitionCard = cut.FindAll("div").First(d => d.ClassList.Contains("cursor-pointer") && d.TextContent.Contains("شروع بررسی تیکت"));
            transitionCard.Should().NotBeNull();            transitionCard.Click();

            // Verify comment box & dynamic field render
            ModalMarkup.Should().Contain("توضیحات و یادداشت عملیات (اختیاری)");            ModalMarkup.Should().Contain("علت بررسی");        }

        [Fact]
        public async Task SubmitTransition_WhenValidationFails_DisplaysErrorMessages()
        {
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ExecuteTransitionDto>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Comment", "ثبت توضیحات برای این انتقال الزامی است.")
                }));

            var cut = Render<TicketTransitionModal>();

            await cut.InvokeAsync(() => cut.Instance.OpenAsync(
                ticketId: 42,
                title: "تیکت تستی",
                currentStatusId: 1,
                workflowId: 1,
                currentWorkflowStatusId: 10
            ));

            // Select transition
            var transitionCard = cut.FindAll("div").First(d => d.ClassList.Contains("cursor-pointer") && d.TextContent.Contains("شروع بررسی تیکت"));
            transitionCard.Click();

            // Submit
            var form = cut.Find("form");
            form.Submit();

            ModalMarkup.Should().Contain("ثبت توضیحات برای این انتقال الزامی است.");            _mockTicketService.Verify(t => t.ExecuteTransitionAsync(It.IsAny<ExecuteTransitionDto>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SubmitTransition_WhenValid_ExecutesTransition_AndInvokesOnSaved()
        {
            bool onSavedCalled = false;
            ExecuteTransitionDto? capturedDto = null;
            int capturedUserId = 0;

            _mockTicketService.Setup(t => t.ExecuteTransitionAsync(It.IsAny<ExecuteTransitionDto>(), It.IsAny<int>()))
                .Callback<ExecuteTransitionDto, int>((dto, uId) =>
                {
                    capturedDto = dto;
                    capturedUserId = uId;
                })
                .Returns(Task.CompletedTask);

            var cut = Render<TicketTransitionModal>(parameters => parameters
                .Add(p => p.OnSaved, () => { onSavedCalled = true; })
            );

            await cut.InvokeAsync(() => cut.Instance.OpenAsync(
                ticketId: 42,
                title: "تیکت تستی",
                currentStatusId: 1,
                workflowId: 1,
                currentWorkflowStatusId: 10
            ));

            // Select transition 102 (بستن سریع)
            var transitionCard = cut.FindAll("div").First(d => d.ClassList.Contains("cursor-pointer") && d.TextContent.Contains("بستن سریع"));
            transitionCard.Click();

            // Submit form
            var form = cut.Find("form");
            form.Submit();

            cut.WaitForAssertion(() =>
            {
                capturedDto.Should().NotBeNull();
                capturedDto.TicketId.Should().Be(42);
                capturedDto.TransitionId.Should().Be(102);
                capturedUserId.Should().Be(1);
                _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
                onSavedCalled.Should().BeTrue();
                cut.Instance.IsVisible.Should().BeFalse();
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CloseAsync_HidesModal()
        {
            var cut = Render<TicketTransitionModal>();

            await cut.InvokeAsync(() => cut.Instance.OpenAsync(
                ticketId: 42,
                title: "تیکت تستی",
                currentStatusId: 1,
                workflowId: 1,
                currentWorkflowStatusId: 10
            ));

            cut.Instance.IsVisible.Should().BeTrue();
            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cut.Instance.IsVisible.Should().BeFalse();        }
    }
}
