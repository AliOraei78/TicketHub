using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using FluentValidation;
using TicketHub.Application.Validations;


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

            // Add bUnit test authorization context
            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("TestUser");



            // Register default Mock IPermissionService for SecuredView components
            var mockPermService = new Mock<TicketHub.Application.Interfaces.IPermissionService>();
            mockPermService.Setup(p => p.HasAccessAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<TicketHub.Application.Enums.PermissionType>()))
                          .ReturnsAsync(true);
            Services.AddSingleton(mockPermService.Object);

            // Register all FluentValidation validators from the Application assembly
            Services.AddValidatorsFromAssembly(typeof(RoleDtoValidator).Assembly);


        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
