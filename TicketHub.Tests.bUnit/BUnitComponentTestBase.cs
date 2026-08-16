using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using TicketHub.Application.Validations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace TicketHub.Tests.bUnit
{
    public class TestModalWrapper : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SectionOutlet>(0);
            builder.AddAttribute(1, "SectionName", "RootModal");
            builder.CloseComponent();
            builder.AddContent(2, ChildContent);
        }
    }

    public class SectionOutletRef
    {
        public IRenderedComponent<TestModalWrapper>? Wrapper { get; set; }
    }

    public static class BUnitModalExtensions
    {
        public static AngleSharp.Dom.IElement Find<T>(this IRenderedComponent<T> cut, string cssSelector) where T : IComponent
        {
            try
            {
                return Bunit.RenderedComponentExtensions.Find(cut, cssSelector);
            }
            catch
            {
                var outletRef = cut.Services.GetService<SectionOutletRef>();
                if (outletRef?.Wrapper != null)
                {
                    return Bunit.RenderedComponentExtensions.Find(outletRef.Wrapper, cssSelector);
                }
                throw;
            }
        }

        public static IReadOnlyList<AngleSharp.Dom.IElement> FindAll<T>(this IRenderedComponent<T> cut, string cssSelector) where T : IComponent
        {
            var list = Bunit.RenderedComponentExtensions.FindAll(cut, cssSelector).ToList();
            var outletRef = cut.Services.GetService<SectionOutletRef>();
            if (outletRef?.Wrapper != null)
            {
                var wrapperList = Bunit.RenderedComponentExtensions.FindAll(outletRef.Wrapper, cssSelector);
                foreach (var el in wrapperList)
                {
                    if (!list.Contains(el)) list.Add(el);
                }
            }
            return list;
        }
    }

    /// <summary>
    /// Base class for all bUnit component tests to enforce DRY principles.
    /// Sets up the common TestContext and common mock services.
    /// </summary>
    public abstract class BUnitComponentTestBase : BunitContext, IDisposable
    {
        private readonly SectionOutletRef _outletRef = new();

        public BUnitComponentTestBase()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("TestUser");

            Services.AddSingleton(_outletRef);

            // Register default Mock IPermissionService for SecuredView components
            var mockPermService = new Mock<TicketHub.Application.Interfaces.IPermissionService>();
            mockPermService.Setup(p => p.HasAccessAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<TicketHub.Application.Enums.PermissionType>()))
                          .ReturnsAsync(true);
            Services.AddSingleton(mockPermService.Object);

            // Register default Mock ICacheService
            var mockCacheService = new Mock<TicketHub.Application.Interfaces.ICacheService>();
            Services.AddSingleton(mockCacheService.Object);

            // Register HttpContextAccessor
            Services.AddHttpContextAccessor();

            // Register all FluentValidation validators from the Application assembly
            Services.AddValidatorsFromAssembly(typeof(RoleDtoValidator).Assembly);
        }

        public IRenderedComponent<TComponent> Render<TComponent>() where TComponent : IComponent
        {
            _outletRef.Wrapper = base.Render<TestModalWrapper>(p => p.AddChildContent<TComponent>());
            return _outletRef.Wrapper.FindComponent<TComponent>();
        }

        public new IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder) where TComponent : IComponent
        {
            _outletRef.Wrapper = base.Render<TestModalWrapper>(p => p.AddChildContent<TComponent>(parameterBuilder));
            return _outletRef.Wrapper.FindComponent<TComponent>();
        }

        public string ModalMarkup => _outletRef.Wrapper?.Markup ?? "";

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
