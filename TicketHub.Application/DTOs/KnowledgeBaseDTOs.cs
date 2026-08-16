namespace TicketHub.Application.DTOs;

public class KbCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorClass { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
    public int DisplayOrder { get; set; }
}

public class KbFaqItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryTitle { get; set; } = string.Empty;
}

public class KbArticleDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryTitle { get; set; } = string.Empty;
    public string CategoryColorClass { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public string LastUpdatedPersian { get; set; } = string.Empty;
    public string LastUpdatedIso { get; set; } = string.Empty;
    public int ReadTimeMinutes { get; set; } = 3;
    public bool IsPopular { get; set; }
    public List<KbFaqItemDto> RelatedFaqs { get; set; } = new();
}
