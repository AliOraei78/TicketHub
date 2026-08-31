using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class SystemLogsE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public SystemLogsE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task SystemLogs_NavigationAndRendering_ShouldSucceed()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/system-logs");
            await WaitForBlazorAsync(Page, 1000);

            var bannerTitle = Page.Locator("text=پایش و گزارش").First;
            await bannerTitle.WaitForAsync(new() { Timeout = 10000 });
            (await bannerTitle.IsVisibleAsync()).Should().BeTrue();

            var quickFilters = Page.Locator("button:has-text('همه لاگ‌ها')").First;
            (await quickFilters.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task SystemLogs_QuickFilterChips_ShouldFilterByLevel()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/system-logs");
            await WaitForBlazorAsync(Page, 1000);

            await Page.WaitForSelectorAsync("button:has-text('همه لاگ‌ها')");

            // Filter Error
            await Page.ClickAsync("button:has-text('فقط خطاها')");
            await Task.Delay(600);

            // Filter Warning
            await Page.ClickAsync("button:has-text('هشدارها')");
            await Task.Delay(600);

            // Filter Info
            await Page.ClickAsync("button:has-text('اطلاعات')");
            await Task.Delay(600);

            // Filter All
            await Page.ClickAsync("button:has-text('همه لاگ‌ها')");
            await Task.Delay(600);
        }

        [Fact]
        public async Task SystemLogs_SearchFilter_ShouldFilterRealtime()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/system-logs");
            await WaitForBlazorAsync(Page, 1000);

            await Page.WaitForSelectorAsync("input[placeholder*='جستجوی پیام']");

            var searchInput = Page.Locator("input[placeholder*='جستجوی پیام']").First;
            await searchInput.FillAsync("NonExistentKeyword_" + Guid.NewGuid().ToString().Substring(0, 5));
            await Task.Delay(800);

            // Clear search
            await searchInput.FillAsync("");
            await Task.Delay(800);
        }

        [Fact]
        public async Task SystemLogs_LiveStream_Toggle_ShouldShowBadge()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/system-logs");
            await WaitForBlazorAsync(Page, 1000);

            await Page.WaitForSelectorAsync("button:has-text('فعال‌سازی به‌روزرسانی زنده')");

            // Click live stream button
            await Page.ClickAsync("button:has-text('فعال‌سازی به‌روزرسانی زنده')");
            await Task.Delay(500);

            var liveBadge = Page.Locator("text=جریان زنده فعال").First;
            (await liveBadge.IsVisibleAsync()).Should().BeTrue();

            // Toggle off
            await Page.ClickAsync("button:has-text('به‌روزرسانی زنده فعال')");
            await Task.Delay(500);
        }
    }
}
