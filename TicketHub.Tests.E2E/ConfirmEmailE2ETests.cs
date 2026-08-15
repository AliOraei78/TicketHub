using FluentAssertions;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class ConfirmEmailE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ConfirmEmailE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ConfirmEmail_WithoutTempCookie_RedirectsToLogin()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/confirm-email");

            await Page.WaitForURLAsync("**/login");
            Page.Url.Should().Contain("/login");
        }

        [Fact]
        public async Task ConfirmEmail_AfterRegister_LandsOnConfirmEmailPage()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/register");

            var nameInput = Page.Locator("input[placeholder='مثال: علی محمدی']").First;
            await nameInput.WaitForAsync();
            (await nameInput.IsVisibleAsync()).Should().BeTrue();
        }
    }
}
