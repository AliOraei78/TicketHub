using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;
using TicketHub.Web.Components.Pages.Auth;
using TicketHub.Web.Components.Pages.Auth.Shared;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class ConfirmEmailTests : BUnitComponentTestBase
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<ConfirmEmail>> _mockLogger;

        public ConfirmEmailTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<ConfirmEmail>>();

            Services.AddSingleton(_mockUserService.Object);
            Services.AddSingleton(_mockEmailService.Object);
            Services.AddSingleton(_mockLogger.Object);
        }

        [Fact]
        public void OnInitialized_NoTempEmailCookie_RedirectsToLogin()
        {
            var navMan = Services.GetRequiredService<NavigationManager>();
            var httpContext = new DefaultHttpContext();

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            navMan.Uri.Should().EndWith("/login");
        }

        [Fact]
        public void OnInitialized_UserNotFoundOrAlreadyConfirmed_RedirectsToLogin()
        {
            var navMan = Services.GetRequiredService<NavigationManager>();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=confirmed@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("confirmed@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 10,
                    Email = "confirmed@tickethub.io",
                    IsConfirmed = true
                });

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            navMan.Uri.Should().EndWith("/login");
        }

        [Fact]
        public void Render_ValidTempEmail_ShowsVerificationForm()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=pending@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("pending@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 15,
                    Email = "pending@tickethub.io",
                    IsConfirmed = false,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(2)
                });

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            cut.Markup.Should().Contain("تایید آدرس ایمیل");
            cut.Markup.Should().Contain("pending@tickethub.io");
            cut.Markup.Should().Contain("تایید و ورود به سامانه");
        }

        [Fact]
        public async Task HandleFormSubmit_InvalidCode_ShowsErrorMessage()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=pending@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("pending@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 15,
                    Email = "pending@tickethub.io",
                    IsConfirmed = false,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(2)
                });

            _mockUserService.Setup(u => u.ConfirmUserAsync(15, "123456"))
                .ReturnsAsync(false);

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            var otpComponent = cut.FindComponent<OtpInput>();
            await cut.InvokeAsync(() => otpComponent.Instance.ValueChanged.InvokeAsync("123456"));

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("کد وارد شده نامعتبر یا اشتباه است");
        }

        [Fact]
        public async Task HandleFormSubmit_SuccessfulCode_ShowsSuccessState()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=pending@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("pending@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 15,
                    Email = "pending@tickethub.io",
                    IsConfirmed = false,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(2)
                });

            _mockUserService.Setup(u => u.ConfirmUserAsync(15, "123456"))
                .ReturnsAsync(true);

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            var otpComponent = cut.FindComponent<OtpInput>();
            await cut.InvokeAsync(() => otpComponent.Instance.ValueChanged.InvokeAsync("123456"));

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("حساب کاربری با موفقیت فعال شد");
        }

        [Fact]
        public void ResendCode_Success_ShowsSuccessNotification()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=pending@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("pending@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 15,
                    Email = "pending@tickethub.io",
                    IsConfirmed = false,
                    TokenExpiration = DateTime.UtcNow.AddSeconds(-10) // expired
                });

            _mockUserService.Setup(u => u.ResendConfirmationCodeAsync("pending@tickethub.io"))
                .ReturnsAsync((true, "کد جدید با موفقیت به ایمیل شما ارسال شد.", 120));

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            cut.Instance.Action = "Resend";

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("کد جدید با موفقیت به ایمیل شما ارسال شد");
        }

        [Fact]
        public void ResendCode_RateLimited_ShowsErrorMessage()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = "TempEmail=pending@tickethub.io";

            _mockUserService.Setup(u => u.GetByEmailAsync("pending@tickethub.io"))
                .ReturnsAsync(new UserDto
                {
                    Id = 15,
                    Email = "pending@tickethub.io",
                    IsConfirmed = false,
                    TokenExpiration = DateTime.UtcNow.AddSeconds(45)
                });

            _mockUserService.Setup(u => u.ResendConfirmationCodeAsync("pending@tickethub.io"))
                .ReturnsAsync((false, "کد تایید قبلی هنوز معتبر است.", 45));

            var cut = Render<ConfirmEmail>(parameters => parameters
                .AddCascadingValue(httpContext)
            );

            cut.Instance.Action = "Resend";

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("کد تایید قبلی هنوز معتبر است");
        }

        [Theory]
        [InlineData("", false, "وارد کردن کد الزامی است")]
        [InlineData("12345", false, "کد باید ۶ رقم باشد")]
        [InlineData("1234567", false, "کد باید ۶ رقم باشد")]
        [InlineData("123456", true, null)]
        public void VerifyViewModel_Validation_Tests(string code, bool expectedValid, string? expectedError)
        {
            var model = new VerifyViewModel
            {
                Code = code
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
