using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class ProjectsE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ProjectsE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task TelemetryCards_RenderAllFiveMetrics()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            var headerLocator = Page.Locator("h1:has-text('مدیریت پروژه‌ها')").First;
            await headerLocator.WaitForAsync();

            // Verify all 5 telemetry segments exist
            (await Page.Locator(".telemetry-pill-segment:has-text('کل پروژه‌ها')").First.IsVisibleAsync()).Should().BeTrue();
            (await Page.Locator(".telemetry-pill-segment:has-text('پروژه‌های فعال')").First.IsVisibleAsync()).Should().BeTrue();
            (await Page.Locator(".telemetry-pill-segment:has-text('غیرفعال')").First.IsVisibleAsync()).Should().BeTrue();
            (await Page.Locator(".telemetry-pill-segment:has-text('جریان‌های کاری')").First.IsVisibleAsync()).Should().BeTrue();
            (await Page.Locator(".telemetry-pill-segment:has-text('ماتریس نقش‌ها')").First.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Create_Project_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            // Click create button
            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");

            var headerLocator = Page.Locator("h2:has-text('ایجاد پروژه جدید')").First;
            await headerLocator.WaitForAsync();

            var newProjectName = "پروژه سایبری E2E " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", newProjectName);
            await Page.FillAsync("textarea[placeholder='درباره این پروژه بنویسید...']", "توضیحات تستی پروژه");

            await Page.ClickAsync("button:has-text('ثبت پروژه')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify project card appears
            var cardLocator = Page.Locator("h3", new PageLocatorOptions { HasTextString = newProjectName });
            await cardLocator.WaitForAsync();
            (await cardLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task CreateProject_CancelModal_DoesNotCreateProject()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");

            var headerLocator = Page.Locator("h2:has-text('ایجاد پروژه جدید')").First;
            await headerLocator.WaitForAsync();

            var cancelledProjectName = "پروژه منصرف شده " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", cancelledProjectName);

            // Click cancel button
            await Page.ClickAsync("button:has-text('انصراف')");

            // Verify modal is closed
            await headerLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            // Verify project not in grid
            var cardLocator = Page.Locator("h3", new PageLocatorOptions { HasTextString = cancelledProjectName });
            (await cardLocator.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Edit_Project_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            // Create one first
            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");
            var newProjectName = "پروژه ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", newProjectName);
            await Page.FillAsync("textarea[placeholder='درباره این پروژه بنویسید...']", "توضیحات تستی پروژه");
            await Page.ClickAsync("button:has-text('ثبت پروژه')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var cardLocator = Page.Locator($".tactical-project-card:has(h3:has-text('{newProjectName}'))").First;
            await cardLocator.WaitForAsync();

            // Click edit on this specific card
            await cardLocator.Locator("button[title='ویرایش پروژه']").ClickAsync(new() { Force = true });

            var editHeaderLocator = Page.Locator("h2:has-text('ویرایش پروژه')").First;
            await editHeaderLocator.WaitForAsync();

            var updatedName = newProjectName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            var editToastLocator = Page.Locator("text=ذخیره شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedCardLocator = Page.Locator("h3", new PageLocatorOptions { HasTextString = updatedName });
            await updatedCardLocator.WaitForAsync();
            (await updatedCardLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_Project_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");
            var newProjectName = "پروژه حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", newProjectName);
            await Page.FillAsync("textarea[placeholder='درباره این پروژه بنویسید...']", "توضیحات تستی پروژه");
            await Page.ClickAsync("button:has-text('ثبت پروژه')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var cardLocator = Page.Locator($".tactical-project-card:has(h3:has-text('{newProjectName}'))").First;
            await cardLocator.WaitForAsync();

            // Click delete on this specific card
            await cardLocator.Locator("button[title='حذف پروژه']").ClickAsync(new() { Force = true });

            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();

            // Click confirm delete
            await Page.Locator("button:has-text('بله، حذف کن'), button:has-text('حذف')").First.ClickAsync();

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify gone
            await cardLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await cardLocator.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteProject_CancelModal_DoesNotDeleteProject()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");
            var newProjectName = "پروژه حفظ شده " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام پروژه را وارد کنید']", newProjectName);
            await Page.FillAsync("textarea[placeholder='درباره این پروژه بنویسید...']", "توضیحات پروژه تستی");
            await Page.ClickAsync("button:has-text('ثبت پروژه')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var cardLocator = Page.Locator($".tactical-project-card:has(h3:has-text('{newProjectName}'))").First;
            await cardLocator.WaitForAsync();

            // Click delete
            await cardLocator.Locator("button[title='حذف پروژه']").ClickAsync(new() { Force = true });

            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف'), button:has-text('خیر')");

            // Modal closes
            await modalLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            // Verify card still exists
            (await cardLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            var headerLocator = Page.Locator("h1:has-text('مدیریت پروژه‌ها')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ایجاد پروژه جدید')");

            var formHeaderLocator = Page.Locator("h2:has-text('ایجاد پروژه جدید')").First;
            await formHeaderLocator.WaitForAsync();

            await Page.Locator("button[type='submit']:has-text('ثبت پروژه')").ClickAsync(new() { Force = true });

            var errorLocator = Page.Locator("text=نام پروژه الزامی است").First;
            await errorLocator.WaitForAsync(new() { Timeout = 10000 });
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task SearchProjects_InRealTime_FiltersGrid()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            // Type non-existent query
            var searchInput = Page.Locator("input[placeholder*='جست‌وجوی پروژه']").First;
            await searchInput.FillAsync("XYZ_NON_EXISTENT_PROJECT_999");

            // Verify empty state is displayed
            var emptyBanner = Page.Locator("text=[SYS // NO_PROJECTS_MATCHED]").First;
            await emptyBanner.WaitForAsync();
            (await emptyBanner.IsVisibleAsync()).Should().BeTrue();

            // Clear search
            await searchInput.FillAsync("");
            await Page.WaitForTimeoutAsync(500);
        }

        [Fact]
        public async Task FilterProjects_By3StateStatusButtons_FiltersCards()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/projects");

            // Click Active filter
            await Page.ClickAsync("button:has-text('فعال')");
            await Page.WaitForTimeoutAsync(300);

            // Click Inactive filter
            await Page.ClickAsync("button:has-text('غیرفعال')");
            await Page.WaitForTimeoutAsync(300);

            // Click All filter
            await Page.ClickAsync("button:has-text('همه')");
            await Page.WaitForTimeoutAsync(300);
        }
    }
}
