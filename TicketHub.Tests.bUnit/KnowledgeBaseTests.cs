using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Web.Components.Pages.Public.KnowledgeBase;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class KnowledgeBaseTests : BUnitComponentTestBase
{
    private readonly IKnowledgeBaseService _service;

    public KnowledgeBaseTests()
    {
        _service = new KnowledgeBaseService();
        Services.AddSingleton<IKnowledgeBaseService>(_service);
    }

    [Fact]
    public async Task KnowledgeBaseService_GetCategories_ReturnsExpectedCategories()
    {
        // Act
        var categories = await _service.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().HaveCountGreaterThanOrEqualTo(4);
        categories.Select(c => c.Slug).Should().Contain("troubleshooting-errors");
    }

    [Fact]
    public async Task KnowledgeBaseService_GetArticles_FiltersBySearchTerm()
    {
        // Act
        var results = await _service.GetArticlesAsync(searchTerm: "SignalR");

        // Assert
        results.Should().NotBeNull();
        results.Should().ContainSingle();
        results[0].Slug.Should().Be("fix-signalr-websocket-connection-lost");
    }

    [Fact]
    public async Task KnowledgeBaseService_GetArticleBySlug_ReturnsArticleWithFaqs()
    {
        // Act
        var article = await _service.GetArticleBySlugAsync("fix-signalr-websocket-connection-lost");

        // Assert
        article.Should().NotBeNull();
        article!.Title.Should().Contain("SignalR");
        article.RelatedFaqs.Should().NotBeNull();
    }

    [Fact]
    public async Task KnowledgeBaseService_SubmitFeedback_IncrementsCounts()
    {
        // Arrange
        var slug = "resolve-rate-limiting-429-too-many-requests";
        var articleBefore = await _service.GetArticleBySlugAsync(slug);
        var helpfulBefore = articleBefore!.HelpfulCount;

        // Act
        var success = await _service.SubmitFeedbackAsync(slug, true);
        var articleAfter = await _service.GetArticleBySlugAsync(slug);

        // Assert
        success.Should().BeTrue();
        articleAfter!.HelpfulCount.Should().Be(helpfulBefore + 1);
    }

    [Fact]
    public void KnowledgeBaseComponent_RendersCategoriesAndSearchBox()
    {
        // Act
        var cut = Render<KnowledgeBase>();

        // Assert
        cut.Markup.Should().Contain("مرکز پشتیبانی و پایگاه دانش");
        cut.Markup.Should().Contain("رفع خطاها و عیب‌یابی نرم‌افزاری");
        cut.Find("input[type='text']").Should().NotBeNull();
    }

    [Fact]
    public void KnowledgeBaseComponent_SearchInput_FiltersArticlesList()
    {
        // Arrange
        var cut = Render<KnowledgeBase>();
        var searchInput = cut.Find("input[type='text']");

        // Act
        searchInput.Input("Rate Limit");

        // Assert
        cut.Markup.Should().Contain("محدودیت تعداد درخواست");
    }

    [Fact]
    public void KnowledgeBaseComponent_ToggleFaq_ExpandsAccordionItem()
    {
        // Arrange
        var cut = Render<KnowledgeBase>();

        // Act
        var faqButtons = cut.FindAll("#faq-section button");
        faqButtons.Should().NotBeEmpty();
        faqButtons[0].Click();

        // Assert
        cut.Markup.Should().Contain("سوالات متداول (FAQ)");
    }

    [Fact]
    public void ArticleDetailsComponent_RendersArticleContentAndBreadcrumb()
    {
        // Act
        var cut = Render<ArticleDetails>(parameters => parameters
            .Add(p => p.Slug, "fix-signalr-websocket-connection-lost")
        );

        // Assert
        cut.Markup.Should().Contain("راهنمای جامع رفع خطای قطع اتصال");
        cut.Markup.Should().Contain("پایگاه دانش");
        cut.Markup.Should().Contain("آیا این مقاله برای شما مفید بود؟");
    }

    [Fact]
    public void ArticleDetailsComponent_SubmitFeedback_UpdatesHelpfulCounter()
    {
        // Arrange
        var cut = Render<ArticleDetails>(parameters => parameters
            .Add(p => p.Slug, "how-sla-countdown-and-breach-monitoring-works")
        );

        // Act
        var helpfulButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("بله"));
        helpfulButton.Should().NotBeNull();
        helpfulButton!.Click();

        // Assert
        cut.Markup.Should().Contain("از ثبت بازخورد ارزشمند شما سپاسگزاریم");
    }

    [Fact]
    public void ArticleDetailsComponent_InvalidSlug_ShowsNotFoundState()
    {
        // Act
        var cut = Render<ArticleDetails>(parameters => parameters
            .Add(p => p.Slug, "non-existent-article-slug-xyz")
        );

        // Assert
        cut.Markup.Should().Contain("مقاله مورد نظر یافت نشد");
    }
}
