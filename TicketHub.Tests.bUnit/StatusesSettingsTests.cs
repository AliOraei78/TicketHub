using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Statuses;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Web.Components.Shared;
using System.Collections.Generic;
using System;
using Fluxor;

namespace TicketHub.Tests.bUnit
{
    public class StatusesSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<StatusState>> _mockState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public StatusesSettingsTests()
        {
            _mockState = new Mock<IState<StatusState>>();
            _mockState.Setup(s => s.Value).Returns(new StatusState(
                false,
                new List<StatusDto>(),
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
        public void StatusesSettings_RendersCorrectly()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new StatusState(
                false,
                new List<StatusDto>
                {
                    new StatusDto { Id = 1, Name = "Open", ColorCode = "#000000", NeedApproval = false, IsActive = true }
                },
                string.Empty,
                null
            ));

            // Act
            var cut = Render<StatusesSettings>();

            // Assert
            cut.FindAll("td").Should().Contain(td => td.InnerHtml.Contains("Open"));
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadStatusesAction>()), Times.Once);
        }

        [Fact]
        public void SubmittingForm_DispatchesSaveAction()
        {
            // Arrange
            var cut = Render<StatusesSettings>();

            // Act
            // Fill form
            cut.Find("input[placeholder='مثال: In Progress']").Change("In Progress");
            cut.Find("input[id='needApproval']").Change(true);
            
            // Submit form
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveStatusAction>(a => a.Status.Name == "In Progress" && a.Status.NeedApproval == true && a.IsEditing == false)), Times.Once);
        }

        [Fact]
        public void SubmittingForm_EmptyName_ShowsValidationError()
        {
            // Arrange
            var cut = Render<StatusesSettings>();

            // Act - Submit without filling Name
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<SaveStatusAction>()), Times.Never);
            var validationMessages = cut.FindAll(".text-red-500");
            validationMessages.Should().NotBeEmpty();        }

        [Fact]
        public void EditStatus_PopulatesForm_And_DispatchesSaveActionWithIsEditingTrue()
        {
            // Arrange
            var status = new StatusDto { Id = 1, Name = "Open", ColorCode = "#000000", NeedApproval = false, IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new StatusState(
                false,
                new List<StatusDto> { status },
                string.Empty,
                null
            ));
            var cut = Render<StatusesSettings>();

            // Act
            // Trigger edit from the DataGrid row
            var editButton = cut.Find("button[title='ویرایش']");
            editButton.Click();

            // Verify form populated (IsEditing = true should change button text)
            var saveButton = cut.Find("button[type='submit']");
            saveButton.TextContent.Should().Contain("ذخیره تغییرات");            cut.Find("input[placeholder='مثال: In Progress']").Attributes["value"]?.Value.Should().Be("Open");
            // Change name
            cut.Find("input[placeholder='مثال: In Progress']").Change("Closed");
            
            // Submit form
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveStatusAction>(a => a.Status.Name == "Closed" && a.Status.Id == 1 && a.IsEditing == true)), Times.Once);
        }

        [Fact]
        public void DeleteStatus_OpensModal_And_DispatchesDeleteActionOnConfirm()
        {
            // Arrange
            var status = new StatusDto { Id = 1, Name = "Open", ColorCode = "#000000", NeedApproval = false, IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new StatusState(
                false,
                new List<StatusDto> { status },
                string.Empty,
                null
            ));
            var cut = Render<StatusesSettings>();

            // Trigger delete from the DataGrid row
            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Should().NotBeNull();            
            // Act
            confirmModal.Find("button.bg-rose-600").Click(); // Click confirm on the modal

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteStatusAction>(a => a.Id == 1)), Times.Once);
        }

        [Fact]
        public void TelemetryCards_RenderTotalActiveAndInactiveCounts()
        {
            var statuses = new List<StatusDto>
            {
                new StatusDto { Id = 1, Name = "Open", IsActive = true },
                new StatusDto { Id = 2, Name = "In Progress", IsActive = true },
                new StatusDto { Id = 3, Name = "Archived", IsActive = false }
            };

            _mockState.Setup(s => s.Value).Returns(new StatusState(false, statuses, string.Empty, null));

            var cut = Render<StatusesSettings>();

            cut.Markup.Should().Contain("کل وضعیت‌ها");
            cut.Markup.Should().Contain("وضعیت‌های فعال");
            cut.Markup.Should().Contain("وضعیت‌های غیرفعال");
        }

        [Fact]
        public void HandleSearch_DispatchesSetSearchAction()
        {
            var cut = Render<StatusesSettings>();

            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Progress"));

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetStatusSearchAction>(a => a.Term == "Progress")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_DispatchesSetFilterStatusAction()
        {
            var cut = Render<StatusesSettings>();

            var activeFilterBtn = cut.Find("button:contains('فعال')");
            activeFilterBtn.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetStatusFilterStatusAction>(a => a.Status == true)), Times.Once);
        }

        [Fact]
        public void DeleteStatus_CancelModal_ClosesWithoutDispatchingDelete()
        {
            var status = new StatusDto { Id = 1, Name = "Open", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new StatusState(false, new List<StatusDto> { status }, string.Empty, null));

            var cut = Render<StatusesSettings>();

            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Cancel
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteStatusAction>()), Times.Never);
        }
    }
}

