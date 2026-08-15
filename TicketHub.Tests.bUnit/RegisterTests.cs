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
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Web.Components.Pages.Auth;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class RegisterTests : BUnitComponentTestBase
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<Register>> _mockLogger;
        private readonly Mock<IDNTCaptchaValidatorService> _mockCaptchaValidator;
        private readonly DefaultHttpContext _httpContext;

        public RegisterTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<Register>>();
            _mockCaptchaValidator = new Mock<IDNTCaptchaValidatorService>();
            _httpContext = new DefaultHttpContext();

            // Default captcha valid
            _mockCaptchaValidator.Setup(c => c.HasRequestValidCaptchaEntry())
                .Returns(true);

            Services.AddSingleton(_mockUserService.Object);
            Services.AddSingleton(_mockLogger.Object);
            Services.AddSingleton(_mockCaptchaValidator.Object);
        }

        [Fact]
        public void Render_RegisterPage_ShowsAllRequiredElements()
        {
            var cut = Render<Register>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Markup.Should().Contain("ایجاد حساب کاربری");
            cut.Markup.Should().Contain("ENROLLMENT // NEW_USER");
            cut.Markup.Should().Contain("نام و نام خانوادگی");
            cut.Markup.Should().Contain("شماره همراه");
            cut.Markup.Should().Contain("آدرس ایمیل سازمانی");
            cut.Markup.Should().Contain("رمز عبور");
            cut.Markup.Should().Contain("تکرار رمز عبور");
            cut.Markup.Should().Contain("کد امنیتی");
            cut.Markup.Should().Contain("تکمیل ثبت‌نام و دریافت کد تایید");
            cut.Markup.Should().Contain("ورود به حساب");
        }

        [Fact]
        public void HandleRegister_InvalidCaptcha_SetsErrorMessage()
        {
            _mockCaptchaValidator.Setup(c => c.HasRequestValidCaptchaEntry())
                .Returns(false);

            var cut = Render<Register>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Find("input[placeholder='مثال: علی محمدی']").Change("علی رضایی");
            cut.Find("input[placeholder='09123456789']").Change("09121112233");
            cut.Find("input[type='email']").Change("ali@tickethub.io");
            cut.FindAll("input[type='password']")[0].Change("SecurePass123!");
            cut.FindAll("input[type='password']")[1].Change("SecurePass123!");

            cut.Find("form").Submit();

            cut.Markup.Should().Contain("کد امنیتی وارد شده نادرست است یا منقضی شده است");
            _mockUserService.Verify(u => u.RegisterUserAsync(It.IsAny<UserDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void HandleRegister_DuplicateEmail_ShowsErrorMessage()
        {
            _mockUserService.Setup(u => u.RegisterUserAsync(It.IsAny<UserDto>(), It.IsAny<string>()))
                .ReturnsAsync((false, "این ایمیل قبلاً ثبت شده است."));

            var cut = Render<Register>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Find("input[placeholder='مثال: علی محمدی']").Change("علی رضایی");
            cut.Find("input[placeholder='09123456789']").Change("09121112233");
            cut.Find("input[type='email']").Change("duplicate@tickethub.io");
            cut.FindAll("input[type='password']")[0].Change("SecurePass123!");
            cut.FindAll("input[type='password']")[1].Change("SecurePass123!");

            cut.Find("form").Submit();

            cut.Markup.Should().Contain("این ایمیل قبلاً ثبت شده است");
        }

        [Fact]
        public void HandleRegister_SuccessfulRegistration_RedirectsToConfirmEmail()
        {
            _mockUserService.Setup(u => u.RegisterUserAsync(It.IsAny<UserDto>(), It.IsAny<string>()))
                .ReturnsAsync((true, null));

            var navMan = Services.GetRequiredService<NavigationManager>();

            var cut = Render<Register>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Find("input[placeholder='مثال: علی محمدی']").Change("کاربر جدید");
            cut.Find("input[placeholder='09123456789']").Change("09129998877");
            cut.Find("input[type='email']").Change("newuser@tickethub.io");
            cut.FindAll("input[type='password']")[0].Change("StrongPass123!");
            cut.FindAll("input[type='password']")[1].Change("StrongPass123!");

            cut.Find("form").Submit();

            navMan.Uri.Should().EndWith("/confirm-email");
        }

        [Fact]
        public void HandleRegister_SystemException_ShowsFriendlyErrorMessage()
        {
            _mockUserService.Setup(u => u.RegisterUserAsync(It.IsAny<UserDto>(), It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("Database timeout"));

            var cut = Render<Register>(parameters => parameters
                .AddCascadingValue(_httpContext)
            );

            cut.Find("input[placeholder='مثال: علی محمدی']").Change("کاربر خطادار");
            cut.Find("input[placeholder='09123456789']").Change("09120000000");
            cut.Find("input[type='email']").Change("error@tickethub.io");
            cut.FindAll("input[type='password']")[0].Change("StrongPass123!");
            cut.FindAll("input[type='password']")[1].Change("StrongPass123!");

            cut.Find("form").Submit();

            cut.Markup.Should().Contain("خطایی در پردازش اطلاعات رخ داد");
        }

        [Theory]
        [InlineData("", "valid@email.com", "09123456789", "ValidPass123!", "ValidPass123!", false, "وارد کردن نام الزامی است")]
        [InlineData("علی", "", "09123456789", "ValidPass123!", "ValidPass123!", false, "وارد کردن ایمیل الزامی است")]
        [InlineData("علی", "invalid-email", "09123456789", "ValidPass123!", "ValidPass123!", false, "فرمت ایمیل وارد شده معتبر نیست")]
        [InlineData("علی", "valid@email.com", "", "ValidPass123!", "ValidPass123!", false, "وارد کردن شماره تماس الزامی است")]
        [InlineData("علی", "valid@email.com", "09123456789", "ValidPass123!", "MismatchPass123!", false, "رمز عبور و تکرار آن مطابقت ندارند")]
        [InlineData("علی", "valid@email.com", "09123456789", "ValidPass123!", "ValidPass123!", true, null)]
        public void RegisterViewModel_Validation_Tests(string name, string email, string phone, string password, string confirmPassword, bool expectedValid, string? expectedError)
        {
            var model = new RegisterViewModel
            {
                Name = name,
                Email = email,
                PhoneNumber = phone,
                Password = password,
                ConfirmPassword = confirmPassword
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
