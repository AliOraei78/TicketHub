using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Categories;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class CategoriesSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<Fluxor.IState<TicketHub.Web.Store.CategoryState>> _mockCatState;
        private readonly Mock<Fluxor.IState<TicketHub.Web.Store.ProjectState>> _mockProjState;
        private readonly Mock<Fluxor.IDispatcher> _mockDispatcher;

        public CategoriesSettingsTests()
        {
            _mockCatState = new Mock<Fluxor.IState<TicketHub.Web.Store.CategoryState>>();
            _mockCatState.Setup(s => s.Value).Returns(new TicketHub.Web.Store.CategoryState(
                false,
                new List<TicketHub.Application.DTOs.CategoryDto>(),
                string.Empty,
                null,
                new List<int>(),
                new List<int>()
            ));
            Services.AddSingleton(_mockCatState.Object);

            _mockProjState = new Mock<Fluxor.IState<TicketHub.Web.Store.ProjectState>>();
            _mockProjState.Setup(s => s.Value).Returns(new TicketHub.Web.Store.ProjectState(
                false,
                new List<TicketHub.Application.DTOs.ProjectDto>(),
                new List<TicketHub.Application.DTOs.WorkflowDto>(),
                new List<TicketHub.Application.DTOs.RoleDto>(),
                string.Empty,
                null
            ));
            Services.AddSingleton(_mockProjState.Object);

            _mockDispatcher = new Mock<Fluxor.IDispatcher>();
            Services.AddSingleton(_mockDispatcher.Object);

            var mockActionSubscriber = new Mock<Fluxor.IActionSubscriber>();
            Services.AddSingleton(mockActionSubscriber.Object);
        }

        [Fact]
        public void Render_CategoriesSettingsPage_Successfully()
        {
            var cut = Render<CategoriesSettings>();

            // Assert page header and grid title
            Assert.NotNull(cut.Find("h3:contains('لیست انواع تیکت')"));
        }

        [Fact]
        public void CategoryForm_RendersInputs_Correctly()
        {
            var cut = Render<CategoriesSettings>();

            // Find form elements in CategoryForm
            Assert.NotNull(cut.Find("input[placeholder='مثال: پشتیبانی فنی']"));
            Assert.NotNull(cut.Find("input[id='categoryIsActive']"));
        }

        [Fact]
        public void EmptyInputs_Trigger_ValidationErrors()
        {
            var cut = Render<CategoriesSettings>();

            // Click submit button in form without filling inputs
            var submitBtn = cut.Find("button[type='submit']");
            submitBtn.Click();

            // Should show validation error for Name
            var validationMsgs = cut.FindAll(".text-red-500");
            Assert.Contains(validationMsgs, el => el.TextContent.Contains("نام دسته‌بندی الزامی است"));
        }

        [Fact]
        public void ValidForm_Submits_Successfully()
        {
            var cut = Render<CategoriesSettings>();

            // Fill inputs
            var nameInput = cut.Find("input[placeholder='مثال: پشتیبانی فنی']");
            nameInput.Change("پشتیبانی فنی");

            var activeInput = cut.Find("input[id='categoryIsActive']");
            activeInput.Change(true);

            // Click submit
            var submitBtn = cut.Find("button[type='submit']");
            submitBtn.Click();

            // Verify dispatcher was called
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<TicketHub.Web.Store.SaveCategoryAction>()), Times.Once);
        }

        [Fact]
        public void Renders_DeleteConfirmationModal_When_DeleteButtonClicked()
        {
            // Update state with one category
            var category = new CategoryDto { Id = 1, Name = "Test Category", IsActive = true };
            
            _mockCatState.Setup(s => s.Value).Returns(new TicketHub.Web.Store.CategoryState(
                false,
                new List<CategoryDto> { category },
                string.Empty,
                null,
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<CategoriesSettings>();

            // Find delete button and click it
            var deleteBtn = cut.Find("button[title='حذف']");
            deleteBtn.Click();

            // Assert modal appears with the correct description
            Assert.NotNull(cut.Find("div.fixed.inset-0")); // Modal background
            var modalBody = cut.Markup;
            Assert.Contains("آیا از حذف «Test Category» مطمئن هستید؟", modalBody);
        }
    }
}
