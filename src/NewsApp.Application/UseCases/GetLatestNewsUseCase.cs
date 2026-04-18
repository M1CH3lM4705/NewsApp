using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;

namespace NewsApp.Application.UseCases;

public class GetLatestNewsUseCase : IGetLatestNewsUseCase
{
    private readonly INewsRepository _newsRepository;
    private readonly IGeminiTranslationService _geminiService;
    private static readonly Dictionary<string, (DateTime Expiry, IEnumerable<NewsArticle> Articles)> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GetLatestNewsUseCase(INewsRepository newsRepository, IGeminiTranslationService geminiService)
    {
        _newsRepository = newsRepository;
        _geminiService = geminiService;
    }

    public async Task<IEnumerable<NewsArticle>> ExecuteAsync(string? category = null, string? query = null, int page = 1, int pageSize = 10)
    {
        var cacheKey = $"{category}-{query}-{page}-{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out var cacheEntry) && cacheEntry.Expiry > DateTime.UtcNow)
        {
            return cacheEntry.Articles;
        }

        var articles = await _newsRepository.GetArticlesAsync(category, query, page, pageSize);
        
        // Chamada ao Gemini para tradução e resumo
        var translatedArticles = await _geminiService.TranslateArticlesAsync(articles);
        var articleList = translatedArticles.ToList();
        
        _cache[cacheKey] = (DateTime.UtcNow.Add(CacheDuration), articleList);
        
        return articleList;
    }
}
