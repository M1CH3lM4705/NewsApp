using Microsoft.Extensions.Logging;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;

namespace NewsApp.Infrastructure.Services;

public class TranslationOrchestrator : IGeminiTranslationService
{
    private readonly GeminiTranslationService _geminiService;
    private readonly OpenRouterTranslationService _openRouterService;
    private readonly ILogger<TranslationOrchestrator> _logger;

    public TranslationOrchestrator(
        GeminiTranslationService geminiService, 
        OpenRouterTranslationService openRouterService, 
        ILogger<TranslationOrchestrator> logger)
    {
        _geminiService = geminiService;
        _openRouterService = openRouterService;
        _logger = logger;
    }

    public async Task<IEnumerable<NewsArticle>> TranslateArticlesAsync(IEnumerable<NewsArticle> articles)
    {
        try
        {
            return await _geminiService.TranslateArticlesAsync(articles);
        }
        catch (ExternalServiceException ex) when (ex.StatusCode == 429)
        {
            _logger.LogWarning("Gemini quota exhausted (429). Failing over to OpenRouter.");
            try
            {
                return await _openRouterService.TranslateArticlesAsync(articles);
            }
            catch (Exception orex)
            {
                _logger.LogError(orex, "OpenRouter failover also failed. Returning original articles.");
                return articles;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini failed with non-quota error. Returning original articles.");
            return articles;
        }
    }
}
