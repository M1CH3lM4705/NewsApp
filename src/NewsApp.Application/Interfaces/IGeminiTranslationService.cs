using NewsApp.Domain.Entities;

namespace NewsApp.Application.Interfaces;

public interface IGeminiTranslationService
{
    Task<IEnumerable<NewsArticle>> TranslateArticlesAsync(IEnumerable<NewsArticle> articles);
}
