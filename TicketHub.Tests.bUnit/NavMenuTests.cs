using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Components.Layout;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class NavMenuTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_ExpandedMode_ShowsBrandNameAndAllNavItems()
        {
            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, false)
                .Add(p => p.IsMobileMenuOpen, false)
            );

            cut.Markup.Should().Contain("TICKETHUB");
            cut.Markup.Should().Contain("COMMAND MATRIX");
            cut.Markup.Should().Contain("داشبورد اصلی");
            cut.Markup.Should().Contain("مدیریت تیکت‌ها");
            cut.Markup.Should().Contain("پروژه‌ها");
            cut.Markup.Should().Contain("مدیریت کاربران");
            cut.Markup.Should().Contain("مدیریت جریان‌های کاری");
            cut.Markup.Should().Contain("لاگ‌های سیستم");
            cut.Markup.Should().Contain("تنظیمات سیستم");
            cut.Markup.Should().Contain("پروفایل من");
            cut.Markup.Should().Contain("خروج از سیستم");
        }

        [Fact]
        public void Render_CollapsedMode_HidesBrandTextAndShowsCompactLayout()
        {
            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, true)
                .Add(p => p.IsMobileMenuOpen, false)
            );

            // Brand title text should not be visible when collapsed
            cut.Markup.Should().NotContain("COMMAND MATRIX");
            cut.Find("aside button[title='باز کردن منو']").Should().NotBeNull();
        }

        [Fact]
        public void ToggleButton_Click_InvokesOnToggleSidebarCallback()
        {
            bool toggleInvoked = false;

            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, false)
                .Add(p => p.OnToggleSidebar, EventCallback.Factory.Create(this, () => toggleInvoked = true))
            );

            var toggleBtn = cut.Find("button[title='بستن منو']");
            toggleBtn.Click();

            toggleInvoked.Should().BeTrue();
        }

        [Fact]
        public void BrandLogoToggle_Click_InvokesOnToggleSidebarCallback()
        {
            bool toggleInvoked = false;

            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, false)
                .Add(p => p.OnToggleSidebar, EventCallback.Factory.Create(this, () => toggleInvoked = true))
            );

            var logoBtn = cut.Find("div.flex.items-center.gap-3 > button");
            logoBtn.Click();

            toggleInvoked.Should().BeTrue();
        }

        [Fact]
        public void MobileCloseButton_Click_InvokesCloseMobileMenuCallback()
        {
            bool closeInvoked = false;

            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, false)
                .Add(p => p.IsMobileMenuOpen, true)
                .Add(p => p.CloseMobileMenu, EventCallback.Factory.Create(this, () => closeInvoked = true))
            );

            var mobileCloseBtn = cut.Find("button.md\\:hidden");
            mobileCloseBtn.Click();

            closeInvoked.Should().BeTrue();
        }

        [Fact]
        public void SecuredView_WhenPermissionDenied_HidesSpecificMenuItem()
        {
            // Create a dedicated mock permission service where /system-logs is denied
            var mockPermService = new Mock<IPermissionService>();
            mockPermService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), "/system-logs", It.IsAny<PermissionType>()))
                .ReturnsAsync(false);
            mockPermService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.Is<string>(k => k != "/system-logs"), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Re-render in this test context
            Services.AddSingleton(mockPermService.Object);

            var cut = Render<NavMenu>(parameters => parameters
                .Add(p => p.IsCollapsed, false)
            );

            // System Logs should be hidden
            cut.Markup.Should().NotContain("لاگ‌های سیستم");
            // Others should be present
            cut.Markup.Should().Contain("مدیریت تیکت‌ها");
        }
    }
}
