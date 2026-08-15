using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Users;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class UsersSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<Fluxor.IState<TicketHub.Web.Store.UserState>> _mockUsrState;
        private readonly Mock<Fluxor.IDispatcher> _mockDispatcher;

        public UsersSettingsTests()
        {
            _mockUsrState = new Mock<Fluxor.IState<TicketHub.Web.Store.UserState>>();
            _mockUsrState.Setup(s => s.Value).Returns(new TicketHub.Web.Store.UserState(
                false,
                new List<TicketHub.Application.DTOs.UserDto>(),
                0,
                new List<TicketHub.Application.DTOs.RoleDto>(),
                new List<TicketHub.Application.DTOs.ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            ));
            Services.AddSingleton(_mockUsrState.Object);

            _mockDispatcher = new Mock<Fluxor.IDispatcher>();
            Services.AddSingleton(_mockDispatcher.Object);

            var mockActionSubscriber = new Mock<Fluxor.IActionSubscriber>();
            Services.AddSingleton(mockActionSubscriber.Object);
        }

        [Fact]
        public void Render_UsersPage_Successfully()
        {
            var cut = Render<Users>();

            // Assert page header
            cut.Find("h1:contains('مدیریت کاربران')").Should().NotBeNull();        }

        [Fact]
        public void UserForm_RendersInputs_Correctly()
        {
            var cut = Render<Users>();

            // Open modal
            var createBtn = cut.Find("button:contains('افزودن کاربر جدید')");
            createBtn.Click();

            // Find form elements in UserFormModal
            cut.Find("input[type='text']").Should().NotBeNull();            cut.Find("input[type='email']").Should().NotBeNull();            cut.Find("input[type='tel']").Should().NotBeNull();            cut.Find("input[type='password']").Should().NotBeNull();        }

        [Fact]
        public void SubmitEmptyForm_ShowsValidationMessages()
        {
            var cut = Render<Users>();

            // Open modal
            var createBtn = cut.Find("button:contains('افزودن کاربر جدید')");
            createBtn.Click();

            // Click submit
            var submitBtn = cut.Find("button[type='submit']");
            submitBtn.Click();

            // Verify validation messages render
            var validationMessages = cut.FindAll(".validation-message, .text-red-500");
            validationMessages.Should().NotBeEmpty();        }

        [Fact]
        public void ValidForm_Submits_Successfully()
        {
            var cut = Render<Users>();

            var createBtn = cut.Find("button:contains('افزودن کاربر جدید')");
            createBtn.Click();

            var inputs = cut.FindAll("input[type='text']");
            // inputs[3] is the user name input
            inputs[3].Change("علی علوی");

            var emailInput = cut.Find("input[type='email']");
            emailInput.Change("ali@test.com");

            var telInput = cut.Find("input[type='tel']");
            telInput.Change("09121111111");

            var passInput = cut.Find("input[type='password']");
            passInput.Change("Test@123");

            // Click submit
            var submitBtn = cut.Find("button[type='submit']");
            submitBtn.Click();

            // Verify dispatcher was called
            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<TicketHub.Web.Store.SaveUserAction>()), Times.Once);
        }

        [Fact]
        public void Renders_DeleteConfirmationModal_When_DeleteButtonClicked()
        {
            // Update state with one user
            var user = new UserDto { Id = 1, Name = "Test User", Email = "test@test.com", IsActive = true };
            
            _mockUsrState.Setup(s => s.Value).Returns(new TicketHub.Web.Store.UserState(
                false,
                new List<UserDto> { user },
                1,
                new List<RoleDto>(),
                new List<ProjectDto>(),
                string.Empty,
                10,
                1,
                null,
                new List<int>(),
                new List<int>()
            ));

            var cut = Render<Users>();

            // Find delete button and click it
            var deleteBtn = cut.Find("button[title='حذف']");
            deleteBtn.Click();

            var confirmModal = cut.FindComponent<TicketHub.Web.Components.Shared.ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue("ConfirmDeleteModal IsOpen should be true");            confirmModal.Instance.Description.Should().Contain("آیا از حذف کاربر Test User مطمئن هستید؟");        }
    }
}
