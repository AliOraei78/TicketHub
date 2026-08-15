using FluentAssertions;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class RegisterE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public RegisterE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_Page_Renders_AllComponents()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/register");

            var title = Page.Locator("text=ایجاد حساب کاربری").First;
            await title.WaitForAsync();
            (await title.IsVisibleAsync()).Should().BeTrue();

            var nameInput = Page.Locator("input[placeholder='مثال: علی محمدی']").First;
            (await nameInput.IsVisibleAsync()).Should().BeTrue();

            var phoneInput = Page.Locator("input[placeholder='09123456789']").First;
            (await phoneInput.IsVisibleAsync()).Should().BeTrue();

            var emailInput = Page.Locator("input[type='email']").First;
            (await emailInput.IsVisibleAsync()).Should().BeTrue();

            var passInputs = Page.Locator("input[type='password']");
            (await passInputs.CountAsync()).Should().BeGreaterThanOrEqualTo(2);

            var captchaInput = Page.Locator("#CaptchaInputText").First;
            (await captchaInput.IsVisibleAsync()).Should().BeTrue();

            var submitBtn = Page.Locator("button:has-text('تکمیل ثبت‌نام و دریافت کد تایید')").First;
            (await submitBtn.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Register_Navigation_To_Login()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/register");

            var loginLink = Page.Locator("a[href='/login']").First;
            await loginLink.WaitForAsync();
            await loginLink.ClickAsync();

            await Page.WaitForURLAsync("**/login");
            Page.Url.Should().Contain("/login");

            var loginHeader = Page.Locator("text=ورود به سامانه").First;
            (await loginHeader.IsVisibleAsync()).Should().BeTrue();
        }
    }
}
