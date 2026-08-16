using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Priorities;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Web.Components.Shared;
using System.Collections.Generic;
using System;
using Fluxor;

namespace TicketHub.Tests.bUnit
{
    public class PrioritiesSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<PriorityState>> _mockState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public PrioritiesSettingsTests()
        {
            _mockState = new Mock<IState<PriorityState>>();
            _mockState.Setup(s => s.Value).Returns(new PriorityState(
                false,
                new List<PriorityDto>(),
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
        public void PrioritiesSettings_RendersCorrectly()
        {
            // Arrange
            _mockState.Setup(s => s.Value).Returns(new PriorityState(
                false,
                new List<PriorityDto>
                {
                    new PriorityDto { Id = 1, Name = "High", ColorCode = "#ff0000", Level = 10, IsActive = true }
                },
                string.Empty,
                null
            ));

            // Act
            var cut = Render<PrioritiesSettings>();

            // Assert
            cut.FindAll("td").Should().Contain(td => td.InnerHtml.Contains("High"));
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadPrioritiesAction>()), Times.Once);
        }

        [Fact]
        public void SubmittingForm_DispatchesSaveAction()
        {
            // Arrange
            var cut = Render<PrioritiesSettings>();

            // Act
            // Find inputs and change them individually to avoid stale elements after re-render
            cut.FindAll("input.appearance-none")[0].Change("Medium"); // Name
            cut.FindAll("input.appearance-none")[1].Change(5);      // Level

            // Submit form
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SavePriorityAction>(a => a.Priority.Name == "Medium" && a.Priority.Level == 5 && a.IsEditing == false)), Times.Once);
        }

        [Fact]
        public void SubmittingForm_EmptyName_ShowsValidationError()
        {
            // Arrange
            var cut = Render<PrioritiesSettings>();

            // Act - Submit without filling Name
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<SavePriorityAction>()), Times.Never);
            var validationMessages = cut.FindAll(".text-red-500");
            validationMessages.Should().NotBeEmpty();
        }

        [Fact]
        public void EditPriority_PopulatesForm_And_DispatchesSaveActionWithIsEditingTrue()
        {
            // Arrange
            var priority = new PriorityDto { Id = 1, Name = "High", ColorCode = "#ff0000", Level = 10, IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PriorityState(
                false,
                new List<PriorityDto> { priority },
                string.Empty,
                null
            ));
            var cut = Render<PrioritiesSettings>();

            // Act
            // Trigger edit from the DataGrid row
            var editButton = cut.Find("button[title='ویرایش']");
            editButton.Click();

            // Verify form populated (IsEditing = true should change button text)
            var saveButton = cut.Find("button[type='submit']");
            saveButton.TextContent.Should().Contain("ذخیره تغییرات");
            var inputs = cut.FindAll("input.appearance-none");
            inputs[0].Attributes["value"]?.Value.Should().Be("High"); inputs[1].Attributes["value"]?.Value.Should().Be("10");
            // Change name (query again to avoid stale element exception)
            cut.FindAll("input.appearance-none")[0].Change("Critical");

            // Submit form
            var form = cut.Find("form");
            form.Submit();

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SavePriorityAction>(a => a.Priority.Name == "Critical" && a.Priority.Id == 1 && a.IsEditing == true)), Times.Once);
        }

        [Fact]
        public void DeletePriority_OpensModal_And_DispatchesDeleteActionOnConfirm()
        {
            // Arrange
            var priority = new PriorityDto { Id = 1, Name = "High", ColorCode = "#ff0000", Level = 10, IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PriorityState(
                false,
                new List<PriorityDto> { priority },
                string.Empty,
                null
            ));
            var cut = Render<PrioritiesSettings>();

            // Trigger delete from the DataGrid row
            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            // Assert Modal opened
            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Should().NotBeNull();
            // Act
            confirmModal.Find("button.bg-rose-600").Click(); // Click confirm on the modal

            // Assert
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeletePriorityAction>(a => a.Id == 1)), Times.Once);
        }

        [Fact]
        public void TelemetryCards_RenderTotalActiveAndInactiveCounts()
        {
            var priorities = new List<PriorityDto>
            {
                new PriorityDto { Id = 1, Name = "High", IsActive = true },
                new PriorityDto { Id = 2, Name = "Medium", IsActive = true },
                new PriorityDto { Id = 3, Name = "Low", IsActive = false }
            };

            _mockState.Setup(s => s.Value).Returns(new PriorityState(false, priorities, string.Empty, null));

            var cut = Render<PrioritiesSettings>();

            cut.Markup.Should().Contain("کل اولویت‌ها");
            cut.Markup.Should().Contain("اولویت‌های فعال");
            cut.Markup.Should().Contain("اولویت‌های غیرفعال");
        }

        [Fact]
        public void HandleSearch_DispatchesSetSearchAction()
        {
            var cut = Render<PrioritiesSettings>();

            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("Critical"));

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetPrioritySearchAction>(a => a.Term == "Critical")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_DispatchesSetFilterStatusAction()
        {
            var cut = Render<PrioritiesSettings>();

            var activeFilterBtn = cut.Find("button:contains('فعال')");
            activeFilterBtn.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetPriorityFilterStatusAction>(a => a.Status == true)), Times.Once);
        }

        [Fact]
        public void DeletePriority_CancelModal_ClosesWithoutDispatchingDelete()
        {
            var priority = new PriorityDto { Id = 1, Name = "High", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new PriorityState(false, new List<PriorityDto> { priority }, string.Empty, null));

            var cut = Render<PrioritiesSettings>();

            var deleteButton = cut.Find("button[title='حذف']");
            deleteButton.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            // Cancel
            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeletePriorityAction>()), Times.Never);
        }
    }
}

