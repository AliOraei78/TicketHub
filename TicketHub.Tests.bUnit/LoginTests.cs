using Bunit;
using DNTCaptcha.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;
using TicketHub.Web.Components.Pages.Auth;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class LoginTests : BUnitComponentTestBase
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<Login>> _mockLogger;
        private readonly Mock<IDNTCaptchaValidatorService> _mockCaptchaValidator;
        private readonly DefaultHttpContext _httpContext;

        public LoginTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<Login>>();
            _mockCaptchaValidator = new Mock<IDNTCaptchaValidatorService>();
            _httpContext = new DefaultHttpContext();

            // Default captcha valid
            _mockCaptchaValidator.Setup(c => c.HasRequestValidCaptchaEntry())
                .Returns(true);

            Services.AddSingleton(_mockUserService.Object);
            Services.AddSingleton(_mockEmailService.Object);
            Services.AddSingleton(_mockLogger.Object);
            Services.AddSingleton(_mockCaptchaValidator.Object);
        }

        [Fact]
        public void Render_LoginPage_ShowsAllRequiredElements()
        {
            var cut = Render<Login>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Markup.Should().Contain("ورود به سامانه");
            cut.Markup.Should().Contain("آدرس ایمیل سازمانی");
            cut.Markup.Should().Contain("رمز عبور");
            cut.Markup.Should().Contain("مرا به خاطر بسپار");
            cut.Markup.Should().Contain("کد امنیتی");
            cut.Markup.Should().Contain("ورود امن به سامانه");
            cut.Markup.Should().Contain("ثبت‌نام در سامانه");
        }

        [Fact]
        public void HandleLogin_InvalidCaptcha_SetsErrorMessage()
        {
            _mockCaptchaValidator.Setup(c => c.HasRequestValidCaptchaEntry())
                .Returns(false);

            var cut = Render<Login>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            var emailInput = cut.Find("input[type='email']");
            emailInput.Change("user@tickethub.io");

            var passInput = cut.Find("input[type='password']");
            passInput.Change("ValidPass123!");

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("کد امنیتی وارد شده نادرست است یا منقضی شده است");
            _mockUserService.Verify(u => u.LoginAsync(It.IsAny<LoginViewModel>()), Times.Never);
        }

        [Fact]
        public void HandleLogin_InvalidCredentials_ShowsErrorMessage()
        {
            _mockUserService.Setup(u => u.LoginAsync(It.IsAny<LoginViewModel>()))
                .ReturnsAsync(new AuthServiceResponse
                {
                    Success = false,
                    ErrorMessage = "ایمیل یا رمز عبور اشتباه است."
                });

            var cut = Render<Login>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            var emailInput = cut.Find("input[type='email']");
            emailInput.Change("user@tickethub.io");

            var passInput = cut.Find("input[type='password']");
            passInput.Change("WrongPass123!");

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("ایمیل یا رمز عبور اشتباه است");
        }

        [Fact]
        public void HandleLogin_InactiveAccount_ShowsErrorMessage()
        {
            _mockUserService.Setup(u => u.LoginAsync(It.IsAny<LoginViewModel>()))
                .ReturnsAsync(new AuthServiceResponse
                {
                    Success = false,
                    ErrorMessage = "حساب کاربری شما غیرفعال می‌باشد."
                });

            var cut = Render<Login>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            var emailInput = cut.Find("input[type='email']");
            emailInput.Change("inactive@tickethub.io");

            var passInput = cut.Find("input[type='password']");
            passInput.Change("ValidPass123!");

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("حساب کاربری شما غیرفعال می‌باشد");
        }

        [Fact]
        public void HandleLogin_RequiresConfirmation_RedirectsToConfirmEmail()
        {
            _mockUserService.Setup(u => u.LoginAsync(It.IsAny<LoginViewModel>()))
                .ReturnsAsync(new AuthServiceResponse
                {
                    Success = false,
                    RequiresConfirmation = true,
                    Email = "unconfirmed@tickethub.io"
                });

            var navMan = Services.GetRequiredService<NavigationManager>();

            var cut = Render<Login>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            var emailInput = cut.Find("input[type='email']");
            emailInput.Change("unconfirmed@tickethub.io");

            var passInput = cut.Find("input[type='password']");
            passInput.Change("ValidPass123!");

            var form = cut.Find("form");
            form.Submit();

            navMan.Uri.Should().EndWith("/confirm-email");
        }

        [Theory]
        [InlineData("", "ValidPass123!", false, "وارد کردن ایمیل الزامی است")]
        [InlineData("invalid-email", "ValidPass123!", false, "فرمت ایمیل وارد شده معتبر نیست")]
        [InlineData("valid@email.com", "", false, "وارد کردن رمز عبور الزامی است")]
        [InlineData("valid@email.com", "ValidPass123!", true, null)]
        [InlineData("guest", "guest", true, null)]
        [InlineData("guest@tickethub.io", "guest", true, null)]
        public void LoginViewModel_Validation_Tests(string email, string password, bool expectedValid, string? expectedError)
        {
            var model = new LoginViewModel
            {
                Email = email,
                Password = password
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            isValid.Should().Be(expectedValid);
            if (!expectedValid && expectedError != null)
            {
                results.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains(expectedError));
            }
        }
    }
}
