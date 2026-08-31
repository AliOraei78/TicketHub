using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Web.Components.Shared;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class QuickStatsCardTests : BUnitComponentTestBase
{
    [Fact]
    public void QuickStatsCard_WhenIsLoadingTrue_RendersCyberSkeleton()
    {
        // Act
        var cut = Render<QuickStatsCard>(parameters => parameters
            .Add(p => p.Title, "منقضی شده")
            .Add(p => p.Value, "0")
            .Add(p => p.ElementType, "void")
            .Add(p => p.IsLoading, true));

        // Assert
        cut.FindAll(".cyber-skeleton-box").Should().NotBeEmpty();
        cut.Markup.Should().NotContain("data-target=\"0\"");
    }

    [Fact]
    public void QuickStatsCard_WhenIsLoadingTransitionsToFalse_RendersCardContent_EvenIfValueIsZero()
    {
        // Arrange: Start in loading state with value "0"
        var cut = Render<QuickStatsCard>(parameters => parameters
            .Add(p => p.Title, "منقضی شده")
            .Add(p => p.Value, "0")
            .Add(p => p.ElementType, "void")
            .Add(p => p.IsLoading, true));

        cut.FindAll(".cyber-skeleton-box").Should().NotBeEmpty();

        // Act: Loading completes, but value remains "0"
        cut.Render(parameters => parameters
            .Add(p => p.IsLoading, false));

        // Assert: Skeleton is removed and content is rendered properly
        cut.FindAll(".cyber-skeleton-box").Should().BeEmpty();
        cut.Markup.Should().Contain("منقضی شده");
        cut.Markup.Should().Contain("data-target=\"0\"");
        cut.Markup.Should().Contain("element-void");
    }

    [Theory]
    [InlineData("water", "element-water")]
    [InlineData("lightning", "element-lightning")]
    [InlineData("toxic", "element-toxic")]
    [InlineData("fire", "element-fire")]
    [InlineData("smoke", "element-smoke")]
    [InlineData("void", "element-void")]
    public void QuickStatsCard_RendersCorrectElementClass(string elementType, string expectedClass)
    {
        // Act
        var cut = Render<QuickStatsCard>(parameters => parameters
            .Add(p => p.Title, "تست")
            .Add(p => p.Value, "5")
            .Add(p => p.ElementType, elementType)
            .Add(p => p.IsLoading, false));

        // Assert
        cut.Find(".element-card").ClassList.Should().Contain(expectedClass);
    }
}
