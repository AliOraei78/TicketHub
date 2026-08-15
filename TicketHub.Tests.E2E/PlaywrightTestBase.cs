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

        protected PlaywrightTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
        }

        public async Task InitializeAsync()
        {
            PlaywrightInstance = await Playwright.CreateAsync();
            Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Context = await Browser.NewContextAsync();
            Page = await Context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await Page.CloseAsync();
            await Context.CloseAsync();
            await Browser.CloseAsync();
            PlaywrightInstance.Dispose();
        }
    }
}
