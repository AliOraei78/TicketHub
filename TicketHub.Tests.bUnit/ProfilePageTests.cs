using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Components.Pages.Main;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class ProfilePageTests : BUnitComponentTestBase
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

        public ProfilePageTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockTicketService = new Mock<ITicketService>();
            _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "Cyber Engineer"),
                new Claim(ClaimTypes.Email, "cyber@tickethub.io"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            var authState = new AuthenticationState(userClaims);
            _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);

            _mockUserService.Setup(u => u.GetByIdAsync(42))
                .ReturnsAsync(new UserDto
                {
                    Id = 42,
                    Name = "علی اورعی",
                    Email = "ali@tickethub.io",
                    PhoneNumber = "09123456789",
                    RoleNames = new List<string> { "مدیر ارشد", "پشتیبان فنی" },
                    CreatedAt = System.DateTime.UtcNow.AddMonths(-6)
                });

            _mockUserService.Setup(u => u.UpdateProfileAsync(42, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((true, null));

            _mockTicketService.Setup(t => t.GetTicketTelemetrySummaryAsync(
                It.IsAny<string?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>(),
                42,
                It.IsAny<ClaimsPrincipal?>()))
                .ReturnsAsync(new TicketTelemetrySummaryDto
                {
                    TotalTickets = 15,
                    InProgressCount = 4,
                    ResolvedCount = 11
                });

            Services.AddSingleton(_mockUserService.Object);
            Services.AddSingleton(_mockTicketService.Object);
            Services.AddSingleton(_mockAuthStateProvider.Object);
        }

        [Fact]
        public void Render_ProfilePage_RendersMasterBanner_And_UserData()
        {
            var cut = Render<Profile>();

            // Verify Master Persona Banner Rendered
            cut.Markup.Should().Contain("علی اورعی");
            cut.Markup.Should().Contain("ali@tickethub.io");
            cut.Markup.Should().Contain("#USR-0042");
            cut.Markup.Should().Contain("مدیر ارشد");
            cut.Markup.Should().Contain("پشتیبان فنی");
        }

        [Fact]
        public void Render_ProfilePage_RendersTelemetryStats()
        {
            var cut = Render<Profile>();

            cut.Markup.Should().Contain("کل تیکت‌های من");
            cut.Markup.Should().Contain("در حال رسیدگی");
            cut.Markup.Should().Contain("حل‌شده و بسته‌شده");
        }

        [Fact]
        public void TabSwitching_Shows_CorrectTabSections()
        {
            var cut = Render<Profile>();

            // Default Tab 1: Identity Info
            cut.Markup.Should().Contain("اطلاعات فردی و شناسنامه کاربری");

            // Switch to Tab 2: Security
            var securityTabBtn = cut.Find("button:contains('امنیت و کلید عبور')");
            securityTabBtn.Click();

            cut.Markup.Should().Contain("مدیریت کلمات عبور و سطح امنیت حساب");

            // Switch to Tab 3: Cockpit HUD Customizer
            var hudTabBtn = cut.Find("button:contains('شخصی‌سازی کاک‌پیت و هود')");
            hudTabBtn.Click();

            cut.Markup.Should().Contain("مرکز سفارشی‌سازی هود و اتمسفر کاک‌پیت");
            cut.Markup.Should().Contain("CYBER_WATER");
            cut.Markup.Should().Contain("NEON_FLAME");
        }

        [Fact]
        public void UpdateIdentity_ValidData_CallsUserServiceAndShowsSuccess()
        {
            var cut = Render<Profile>();

            var nameInput = cut.Find("input[placeholder='مثال: علی اورعی']");
            nameInput.Change("علی اورعی جدید");

            var form = cut.Find("form");
            form.Submit();

            _mockUserService.Verify(u => u.UpdateProfileAsync(42, "علی اورعی جدید", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
            cut.Markup.Should().Contain("اطلاعات حساب کاربری شما با موفقیت در پایگاه داده مرکزی بروزرسانی گردید");
        }

        [Fact]
        public void UpdateIdentity_EmptyName_ShowsValidationError()
        {
            var cut = Render<Profile>();

            var nameInput = cut.Find("input[placeholder='مثال: علی اورعی']");
            nameInput.Change("");

            var form = cut.Find("form");
            form.Submit();

            _mockUserService.Verify(u => u.UpdateProfileAsync(42, "", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
            cut.Markup.Should().Contain("وارد کردن نام الزامی است");
        }

        [Fact]
        public void GenerateStrongPassword_GeneratesSecurePassword()
        {
            var cut = Render<Profile>();

            // Go to Security Tab
            var securityTabBtn = cut.Find("button:contains('امنیت و کلید عبور')");
            securityTabBtn.Click();

            // Click Auto Password Generator Button
            var genBtn = cut.Find("button:contains('تولید رمز عبور فوق‌العاده امن')");
            genBtn.Click();

            cut.Markup.Should().Contain("رمز عبور فوق‌العاده امن تولید شد");
        }

        [Fact]
        public void UpdatePassword_WhenCurrentPasswordIncorrect_ShowsErrorMessage()
        {
            _mockUserService.Setup(u => u.UpdateProfileAsync(42, It.IsAny<string>(), It.IsAny<string>(), "WrongPass", "NewStrongPass123!"))
                .ReturnsAsync((false, "رمز عبور فعلی نادرست است."));

            var cut = Render<Profile>();

            // Go to Security Tab
            var securityTabBtn = cut.Find("button:contains('امنیت و کلید عبور')");
            securityTabBtn.Click();

            var currentPassInput = cut.Find("input[placeholder='••••••••']");
            currentPassInput.Change("WrongPass");

            var newPassInput = cut.Find("input[placeholder='رمز جدید...']");
            newPassInput.Change("NewStrongPass123!");

            var confirmPassInput = cut.Find("input[placeholder='تکرار رمز...']");
            confirmPassInput.Change("NewStrongPass123!");

            var form = cut.Find("form");
            form.Submit();

            cut.Markup.Should().Contain("رمز عبور فعلی نادرست است");
        }

        [Fact]
        public void CockpitHUD_AuraSelection_ChangesAura()
        {
            var cut = Render<Profile>();

            // Switch to Cockpit HUD tab
            var hudTabBtn = cut.Find("button:contains('شخصی‌سازی کاک‌پیت و هود')");
            hudTabBtn.Click();

            var flameAuraBtn = cut.Find("button:contains('NEON_FLAME')");
            flameAuraBtn.Click();

            cut.Markup.Should().Contain("پالت رنگی کاک‌پیت به FIRE تغییر یافت");
        }
    }
}
