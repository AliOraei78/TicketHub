using FluentAssertions;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class NavMenuE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public NavMenuE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task NavMenu_Desktop_ToggleCollapseAndExpand()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/");

            var toggleButton = Page.Locator("button[title='بستن منو']").First;
            await toggleButton.WaitForAsync(new() { Timeout = 10000 });

            // Click to collapse
            await toggleButton.ClickAsync();
            await Task.Delay(400);

            var expandButton = Page.Locator("button[title='باز کردن منو']").First;
            (await expandButton.IsVisibleAsync()).Should().BeTrue();

            // Click to expand back
            await expandButton.ClickAsync();
            await Task.Delay(400);

            (await toggleButton.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task NavMenu_NavigateTo_MainSections()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/");

            // Navigate to Tickets
            await Page.ClickAsync("aside a[href='tickets']");
            await Page.WaitForURLAsync("**/tickets");
            Page.Url.Should().Contain("/tickets");

            // Navigate to Projects
            await Page.ClickAsync("aside a[href='projects']");
            await Page.WaitForURLAsync("**/projects");
            Page.Url.Should().Contain("/projects");

            // Navigate to Users
            await Page.ClickAsync("aside a[href='users']");
            await Page.WaitForURLAsync("**/users");
            Page.Url.Should().Contain("/users");

            // Navigate to Profile
            await Page.ClickAsync("aside a[href='/profile']");
            await Page.WaitForURLAsync("**/profile");
            Page.Url.Should().Contain("/profile");
        }

        [Fact]
        public async Task NavMenu_SettingsAccordion_ExpandAndNavigate()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await WaitForBlazorAsync(Page, 1000);

            var desktopAside = Page.Locator("aside").First;
            await desktopAside.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            // Navigate to Roles
            var rolesLink = desktopAside.Locator("a[href='settings/roles']").First;
            if (!await rolesLink.IsVisibleAsync())
            {
                var settingsBtn = desktopAside.Locator("button:has-text('تنظیمات سیستم')").First;
                await settingsBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 600);
            }
            await rolesLink.ClickAsync(new() { Force = true });
            await Page.WaitForURLAsync("**/settings/roles", new() { WaitUntil = WaitUntilState.Commit });
            Page.Url.Should().Contain("/settings/roles");

            // Navigate to Statuses
            var statusesLink = desktopAside.Locator("a[href='settings/statuses']").First;
            if (!await statusesLink.IsVisibleAsync())
            {
                var settingsBtn = desktopAside.Locator("button:has-text('تنظیمات سیستم')").First;
                await settingsBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 600);
            }
            await statusesLink.ClickAsync(new() { Force = true });
            await Page.WaitForURLAsync("**/settings/statuses", new() { WaitUntil = WaitUntilState.Commit });
            Page.Url.Should().Contain("/settings/statuses");

            // Navigate to Categories
            var categoriesLink = desktopAside.Locator("a[href='settings/categories']").First;
            if (!await categoriesLink.IsVisibleAsync())
            {
                var settingsBtn = desktopAside.Locator("button:has-text('تنظیمات سیستم')").First;
                await settingsBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 600);
            }
            await categoriesLink.ClickAsync(new() { Force = true });
            await Page.WaitForURLAsync("**/settings/categories", new() { WaitUntil = WaitUntilState.Commit });
            Page.Url.Should().Contain("/settings/categories");
        }
    }
}
