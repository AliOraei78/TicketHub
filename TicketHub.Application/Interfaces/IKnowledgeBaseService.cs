using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IKnowledgeBaseService
{
    Task<List<KbCategoryDto>> GetCategoriesAsync();
    Task<List<KbArticleDto>> GetArticlesAsync(string? categorySlug = null, string? tag = null, string? searchTerm = null);
    Task<KbArticleDto?> GetArticleBySlugAsync(string slug);
    Task<List<KbArticleDto>> GetPopularArticlesAsync(int count = 6);
    Task<List<KbArticleDto>> GetRelatedArticlesAsync(string slug, string categorySlug, int count = 3);
    Task<List<KbFaqItemDto>> GetFaqsAsync(string? categorySlug = null);
    Task<bool> SubmitFeedbackAsync(string slug, bool isHelpful);
}
