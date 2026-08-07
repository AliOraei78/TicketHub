using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;

namespace TicketHub.Tests.bUnit
{
    /// <summary>
    /// Base class for all bUnit component tests to enforce DRY principles.
    /// Sets up the common TestContext and common mock services.
    /// </summary>
    public abstract class BUnitComponentTestBase : BunitContext, IDisposable
    {
        public BUnitComponentTestBase()
        {
            // Initialize common test context settings here
            // e.g., Register Fluxor if the component requires state
            // Services.AddFluxor(options => options.ScanAssemblies(typeof(TicketHub.Web.Program).Assembly));
            
            // Standard mocks for routing, JS interop, etc. can be added here
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
