using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    public abstract class PlaywrightTestBase : IAsyncLifetime
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected IPlaywright PlaywrightInstance { get; private set; } = default!;
        protected IBrowser Browser { get; private set; } = default!;
        protected IBrowserContext Context { get; private set; } = default!;
        protected IPage Page { get; private set; } = default!;

        protected virtual bool AutoAuthenticate => true;

        protected PlaywrightTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
        }

        public virtual async Task InitializeAsync()
        {
            PlaywrightInstance = await Playwright.CreateAsync();
            Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
            });
            Page = await Context.NewPageAsync();
            Page.SetDefaultTimeout(15000);
            Page.SetDefaultNavigationTimeout(15000);

            if (AutoAuthenticate)
            {
                // Establish active authentication session for authorized E2E tests
                await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            }
        }

        protected static Task WaitForBlazorAsync(int ms = 800) => Task.Delay(ms);
        protected static Task WaitForBlazorAsync(IPage page, int ms = 800) => Task.Delay(ms);

        public async Task DisposeAsync()
        {
            await Page.CloseAsync();
            await Context.CloseAsync();
            await Browser.CloseAsync();
            PlaywrightInstance.Dispose();
        }
    }
}
