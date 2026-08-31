using FluentAssertions;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class ProfileE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ProfileE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Profile_View_MasterPersonaAndTelemetry()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/profile");

            var personaHeader = Page.Locator("h1").First;
            await personaHeader.WaitForAsync();
            await WaitForBlazorAsync(Page, 1000);

            var totalTicketsCard = Page.Locator("text=کل تیکت‌های من").First;
            (await totalTicketsCard.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Profile_TabSwitching_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/profile");

            // Default: Identity Tab
            var identityHeader = Page.Locator("h3:has-text('اطلاعات فردی و شناسنامه کاربری')").First;
            await identityHeader.WaitForAsync();
            await WaitForBlazorAsync(Page, 1000);
            (await identityHeader.IsVisibleAsync()).Should().BeTrue();

            // Switch to Security Tab
            await Page.ClickAsync("button:has-text('امنیت و کلید عبور')");
            var securityHeader = Page.Locator("h3:has-text('مدیریت کلمات عبور و سطح امنیت حساب')").First;
            await securityHeader.WaitForAsync();
            (await securityHeader.IsVisibleAsync()).Should().BeTrue();

            // Switch to Themes Tab
            await Page.ClickAsync("button:has-text('شخصی‌سازی پوسته و تم')");
            var hudHeader = Page.Locator("h3:has-text('شخصی‌سازی پوسته و ظاهر سامانه')").First;
            await hudHeader.WaitForAsync();
            (await hudHeader.IsVisibleAsync()).Should().BeTrue();

            // Switch back to Identity Tab
            await Page.ClickAsync("button:has-text('شناسنامه و مشخصات فردی')");
            await identityHeader.WaitForAsync();
            (await identityHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Profile_UpdateIdentity_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/profile");

            var nameInput = Page.Locator("input[placeholder='مثال: علی اورعی']").First;
            await nameInput.WaitForAsync();
            await WaitForBlazorAsync(Page, 1000);
            await nameInput.FillAsync("مدیر کل سامانه تیکت‌هاب");

            await Page.ClickAsync("button:has-text('ذخیره تغییرات شناسنامه')");

            var successBanner = Page.Locator("text=اطلاعات حساب کاربری شما با موفقیت در پایگاه داده مرکزی بروزرسانی گردید").First;
            await successBanner.WaitForAsync(new() { Timeout = 10000 });
            (await successBanner.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Profile_GenerateStrongPassword_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/profile");

            var identityHeader = Page.Locator("h3:has-text('اطلاعات فردی و شناسنامه کاربری')").First;
            await identityHeader.WaitForAsync();
            await WaitForBlazorAsync(Page, 1000);

            // Go to Security Tab
            await Page.ClickAsync("button:has-text('امنیت و کلید عبور')");
            var securityHeader = Page.Locator("h3:has-text('مدیریت کلمات عبور و سطح امنیت حساب')").First;
            await securityHeader.WaitForAsync();

            // Click Auto Generate
            await Page.ClickAsync("button:has-text('تولید رمز عبور')");

            var statusMsg = Page.Locator("text=رمز عبور فوق‌العاده امن تولید شد").First;
            await statusMsg.WaitForAsync(new() { Timeout = 10000 });
            (await statusMsg.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Profile_CockpitHUD_AuraChange_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/profile");

            var identityHeader = Page.Locator("h3:has-text('اطلاعات فردی و شناسنامه کاربری')").First;
            await identityHeader.WaitForAsync();
            await WaitForBlazorAsync(Page, 1000);

            // Go to Themes tab
            await Page.ClickAsync("button:has-text('شخصی‌سازی پوسته و تم')");
            var hudHeader = Page.Locator("h3:has-text('شخصی‌سازی پوسته و ظاهر سامانه')").First;
            await hudHeader.WaitForAsync();

            // Click Flame aura
            var flameBtn = Page.Locator("button:has-text('نارنجی تابناک (Flame)')").First;
            await flameBtn.WaitForAsync();
            await flameBtn.ClickAsync();

            var statusMsg = Page.Locator("text=پالت رنگی سامانه به FIRE تغییر یافت").First;
            await statusMsg.WaitForAsync(new() { Timeout = 10000 });
            (await statusMsg.IsVisibleAsync()).Should().BeTrue();
        }
    }
}
