using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
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
            Assert.Contains("علی اورعی", cut.Markup);
            Assert.Contains("ali@tickethub.io", cut.Markup);
            Assert.Contains("#USR-0042", cut.Markup);
            Assert.Contains("مدیر ارشد", cut.Markup);
            Assert.Contains("پشتیبان فنی", cut.Markup);
        }

        [Fact]
        public void TabSwitching_Shows_CorrectTabSections()
        {
            var cut = Render<Profile>();

            // Default Tab 1: Identity Info
            Assert.Contains("اطلاعات فردی و شناسنامه کاربری", cut.Markup);

            // Switch to Tab 2: Security
            var securityTabBtn = cut.Find("button:contains('امنیت و کلید عبور')");
            securityTabBtn.Click();

            Assert.Contains("مدیریت کلمات عبور و سطح امنیت حساب", cut.Markup);

            // Switch to Tab 3: Cockpit HUD Customizer
            var hudTabBtn = cut.Find("button:contains('شخصی‌سازی کاک‌پیت و هود')");
            hudTabBtn.Click();

            Assert.Contains("مرکز سفارشی‌سازی هود و اتمسفر کاک‌پیت", cut.Markup);
            Assert.Contains("CYBER_WATER", cut.Markup);
            Assert.Contains("NEON_FLAME", cut.Markup);
        }
    }
}
