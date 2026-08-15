using FluentAssertions;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class LoginE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public LoginE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Login_Page_Renders_AllComponents()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/login");

            var title = Page.Locator("h2:visible:has-text('ورود به سامانه')").First;
            await title.WaitForAsync();
            (await title.IsVisibleAsync()).Should().BeTrue();

            var emailInput = Page.Locator("input[type='email']").First;
            (await emailInput.IsVisibleAsync()).Should().BeTrue();

            var passInput = Page.Locator("input[type='password']").First;
            (await passInput.IsVisibleAsync()).Should().BeTrue();

            var rememberMe = Page.Locator("#rememberMeCheckbox").First;
            (await rememberMe.IsVisibleAsync()).Should().BeTrue();

            var captchaInput = Page.Locator("#CaptchaInputText").First;
            (await captchaInput.IsVisibleAsync()).Should().BeTrue();

            var submitBtn = Page.Locator("button:has-text('ورود امن به سامانه')").First;
            (await submitBtn.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Login_Navigation_To_Register()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/login");

            var registerLink = Page.Locator("a[href='/register']").First;
            await registerLink.WaitForAsync();
            await registerLink.ClickAsync();

            await Page.WaitForURLAsync("**/register");
            Page.Url.Should().Contain("/register");

            var registerHeader = Page.Locator("text=ثبت‌نام").First;
            (await registerHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Login_Dev_Bypass_Establishes_Session()
        {
            // Dev auto login route
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");

            // Verify landing on dashboard with authenticated session
            await Page.WaitForURLAsync($"{Factory.ServerAddress}/");
            Page.Url.Should().EndWith("/");

            var brandText = Page.Locator("text=TICKETHUB").First;
            await brandText.WaitForAsync();
            (await brandText.IsVisibleAsync()).Should().BeTrue();
        }
    }
}
