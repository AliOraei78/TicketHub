using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class DashboardE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DashboardE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }


        [Fact]
        public async Task Dashboard_LoadsSuccessfully_DisplaysWelcomeBannerAndQuickStatsCards()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");

            // 1. Verify Welcome Banner
            var bannerHeader = Page.Locator("h1:has-text('خوش آمدید')");
            await bannerHeader.WaitForAsync(new() { Timeout = 15_000 });
            (await bannerHeader.IsVisibleAsync()).Should().BeTrue();
            var bannerSubtitle = Page.Locator("text=مرکز پایش و حل مشکلات بازیکنان");
            await bannerSubtitle.WaitForAsync(new() { Timeout = 5_000 });
            (await bannerSubtitle.IsVisibleAsync()).Should().BeTrue();
            var createTicketBtn = Page.Locator("button:has-text('ثبت کوئست / تیکت جدید')");
            (await createTicketBtn.IsVisibleAsync()).Should().BeTrue();
            // 2. Verify 6 QuickStats Cards
            var allCard = Page.Locator("div:has-text('کل تیکت‌ها'):has-text('🌊 ALL')").First;
            await allCard.WaitForAsync(new() { Timeout = 5_000 });
            (await allCard.IsVisibleAsync()).Should().BeTrue();
            var newCard = Page.Locator("div:has-text('اقدام نشده'):has-text('⚡ NEW')").First;
            (await newCard.IsVisibleAsync()).Should().BeTrue();
            var activeCard = Page.Locator("div:has-text('در حال بررسی'):has-text('🧪 ACTIVE')").First;
            (await activeCard.IsVisibleAsync()).Should().BeTrue();
            var criticalCard = Page.Locator("div:has-text('بحرانی'):has-text('🔥 CRITICAL')").First;
            (await criticalCard.IsVisibleAsync()).Should().BeTrue();
            var overdueCard = Page.Locator("div:has-text('منقضی شده'):has-text('🌀 OVERDUE')").First;
            (await overdueCard.IsVisibleAsync()).Should().BeTrue();
            var resolvedCard = Page.Locator("div:has-text('خاتمه یافته'):has-text('💨 RESOLVED')").First;
            (await resolvedCard.IsVisibleAsync()).Should().BeTrue();
            // 3. Verify 4 Analytics Chart Cards
            var trendChartCard = Page.Locator("h3:has-text('روند ورودی تیکت‌ها (۷ روز گذشته)')");
            (await trendChartCard.IsVisibleAsync()).Should().BeTrue();
            var priorityChartCard = Page.Locator("h3:has-text('توزیع تیکت‌ها بر اساس اولویت و رنک')");
            (await priorityChartCard.IsVisibleAsync()).Should().BeTrue();
            var projectChartCard = Page.Locator("h3:has-text('سهم بخش‌ها و قلمروها از کل تیکت‌ها')");
            (await projectChartCard.IsVisibleAsync()).Should().BeTrue();
            var slaChartCard = Page.Locator("h3:has-text('نوار سلامت و پاسخگویی به موقع (SLA HP)')");
            (await slaChartCard.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task TrendChart_Click_OpensZoomModal_AndClosesViaCloseButton()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Click on Trend Chart Card
            var trendCard = Page.Locator("div.element-card:has(h3:has-text('روند ورودی تیکت‌ها'))").First;
            await trendCard.WaitForAsync(new() { Timeout = 5_000 });
            await trendCard.ClickAsync();

            // Verify Zoom Modal opens
            var modalTitle = Page.Locator("h3:has-text('تحلیل جامع روند ورودی تیکت‌ها')");
            await modalTitle.WaitForAsync(new() { Timeout = 10_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeTrue();
            var telemetryBadge = Page.Locator("text=[SYS-TELEMETRY // LIVE_FEED]");
            (await telemetryBadge.IsVisibleAsync()).Should().BeTrue();
            // Close via "بستن پنجره" button
            var closeButton = Page.Locator("button:has-text('بستن پنجره')");
            await closeButton.ClickAsync();

            await modalTitle.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task PriorityChart_Click_OpensPriorityModal_AndClosesViaXButton()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Click on Priority Distribution Card
            var priorityCard = Page.Locator("div.element-card:has(h3:has-text('توزیع تیکت‌ها بر اساس اولویت'))").First;
            await priorityCard.WaitForAsync(new() { Timeout = 5_000 });
            await priorityCard.ClickAsync();

            // Verify Priority Zoom Modal opens
            var modalTitle = Page.Locator("h3:has-text('توزیع تفکیکی اولویت‌های سیستم')");
            await modalTitle.WaitForAsync(new() { Timeout = 10_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeTrue();
            // Close via X button in header
            var xButton = Page.Locator("div.modal-hud-chassis button:has(svg)").First;
            await xButton.ClickAsync();

            await modalTitle.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task ProjectWorkloadChart_Click_OpensProjectModal_AndClosesSuccessfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Click on Project Workload Card
            var projectCard = Page.Locator("div.element-card:has(h3:has-text('سهم بخش‌ها و قلمروها'))").First;
            await projectCard.WaitForAsync(new() { Timeout = 5_000 });
            await projectCard.ClickAsync();

            // Verify Project Zoom Modal opens
            var modalTitle = Page.Locator("h3:has-text('سهم پروژه‌ها از لود کاری سیستم')");
            await modalTitle.WaitForAsync(new() { Timeout = 10_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeTrue();
            // Close via "بستن پنجره"
            var closeButton = Page.Locator("button:has-text('بستن پنجره')");
            await closeButton.ClickAsync();

            await modalTitle.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task SlaHealthChart_Click_OpensSlaModal_AndClosesSuccessfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Click on SLA Health Card
            var slaCard = Page.Locator("div.element-card:has(h3:has-text('نوار سلامت و پاسخگویی به موقع'))").First;
            await slaCard.WaitForAsync(new() { Timeout = 5_000 });
            await slaCard.ClickAsync();

            // Verify SLA Zoom Modal opens
            var modalTitle = Page.Locator("h3:has-text('پایش دقیق شاخص زمان‌بندی (SLA)')");
            await modalTitle.WaitForAsync(new() { Timeout = 10_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeTrue();
            var slaComplianceText = Page.Locator("text=میزان پایبندی به زمان‌بندی (SLA)");
            (await slaComplianceText.IsVisibleAsync()).Should().BeTrue();
            // Close via "بستن پنجره"
            var closeButton = Page.Locator("button:has-text('بستن پنجره')");
            await closeButton.ClickAsync();

            await modalTitle.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            (await modalTitle.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task CreateTicketModal_CanBeOpenedFromDashboard_AndCancelled()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Click "ثبت کوئست / تیکت جدید" button
            var createBtn = Page.Locator("button:has-text('ثبت کوئست / تیکت جدید')");
            await createBtn.ClickAsync();

            // Verify TicketFormModal opens in SectionOutlet RootModal
            var formHeader = Page.Locator("h2:has-text('ایجاد تیکت پشتیبانی جدید')");
            await formHeader.WaitForAsync(new() { Timeout = 10_000 });
            (await formHeader.IsVisibleAsync()).Should().BeTrue();
            var titleInput = Page.Locator("input[placeholder='یک عنوان کوتاه بنویسید']");
            (await titleInput.IsVisibleAsync()).Should().BeTrue();
            // Click "انصراف" button
            var cancelBtn = Page.Locator("button:has-text('انصراف')");
            await cancelBtn.ClickAsync();

            await formHeader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            (await formHeader.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task CreateTicketFromDashboard_SubmitsSuccessfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/");
            await Page.WaitForSelectorAsync("h1:has-text('خوش آمدید')", new() { Timeout = 15_000 });

            // Open Create Ticket Modal
            await Page.ClickAsync("button:has-text('ثبت کوئست / تیکت جدید')");
            var formHeader = Page.Locator("h2:has-text('ایجاد تیکت پشتیبانی جدید')");
            await formHeader.WaitForAsync(new() { Timeout = 10_000 });
            await WaitForBlazorAsync(800);

            // Fill Ticket Title & Description
            var testTitle = "تیکت داشبورد E2E " + Guid.NewGuid().ToString()[..5];
            await Page.FillAsync("input[placeholder='یک عنوان کوتاه بنویسید']", testTitle);
            await Page.FillAsync("textarea[placeholder='جزئیات مشکل یا درخواست خود را بنویسید...']", "توضیحات تست ثبت تیکت از روی داشبورد");

            // Select Project
            var projectRoot = Page.Locator("div.relative:has(> label:has-text('پروژه مربوطه'))");
            await projectRoot.Locator(".field-spark-wrap div.cursor-pointer").First.ClickAsync();
            await WaitForBlazorAsync(500);
            await projectRoot.Locator(".dropdown-menu-container div.cursor-pointer:has-text('عمومی')").First.ClickAsync();
            await WaitForBlazorAsync(500);

            // Select Priority
            var priorityRoot = Page.Locator("div.relative:has(> label:has-text('اولویت'))");
            await priorityRoot.Locator(".field-spark-wrap div.cursor-pointer").First.ClickAsync();
            await WaitForBlazorAsync(500);
            await priorityRoot.Locator(".dropdown-menu-container div.cursor-pointer:has-text('متوسط')").First.ClickAsync();
            await WaitForBlazorAsync(500);

            // Submit Ticket
            var submitButton = Page.Locator("button[type='submit']:has-text('ثبت تیکت')");
            await submitButton.ClickAsync();

            // Verify modal closes
            await formHeader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
            (await formHeader.IsVisibleAsync()).Should().BeFalse();        }
    }
}
