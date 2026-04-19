using NewsApp.Application.DTOs;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using System.Collections.Concurrent;

namespace NewsApp.Application.UseCases;

public class GetLatestNewsUseCase : IGetLatestNewsUseCase
{
    private readonly INewsRepository _newsRepository;
    private readonly IGeminiTranslationService _geminiService;
    private static readonly ConcurrentDictionary<string, (DateTime Expiry, IEnumerable<NewsArticle> Articles)> _cache =
    new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GetLatestNewsUseCase(INewsRepository newsRepository, IGeminiTranslationService geminiService)
    {
        _newsRepository = newsRepository;
        _geminiService = geminiService;
    }

    public async Task<IEnumerable<NewsArticle>> ExecuteAsync(GetLatestNewsRequest request)
    {
        var cacheKey = $"{request.Category}-{request.Query}-{request.Page}-{request.PageSize}";
        
        if (_cache.TryGetValue(cacheKey, out var cacheEntry) && cacheEntry.Expiry > DateTime.UtcNow)
        {
            return cacheEntry.Articles;
        }

        var articles = await _newsRepository.GetArticlesAsync(request.Category, request.Query, request.Page, request.PageSize);
        
        // Chamada ao Gemini para tradução e resumo
        var translatedArticles = await _geminiService.TranslateArticlesAsync(articles);
        var articleList = translatedArticles.ToList();
        
        _cache[cacheKey] = (DateTime.UtcNow.Add(CacheDuration), articleList);
        
        return articleList;
    }
}
