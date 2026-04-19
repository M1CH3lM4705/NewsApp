using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.Gemini;

namespace NewsApp.Infrastructure.Services;

public abstract class BaseTranslationService : IGeminiTranslationService
{
    protected readonly IMemoryCache Cache;
    protected readonly ILogger Logger;
    protected abstract string ServiceName { get; }

    protected const string SystemPrompt = "Você é um tradutor jornalístico. Traduza os títulos para português e gere resumos de no máximo 3 linhas em português. " +
                                           "Receba uma lista de notícias e retorne um objeto JSON contendo um array chamado 'translations'. " +
                                           "Cada item do array deve ter: 'url' (id único), 'title' e 'summary'.";

    protected static string GetUserPrompt(List<NewsArticle> articles)
    {
        var articlesData = articles.Select(a => new { a.Url, a.Title, a.Description });
        return $"Notícias:\n{JsonSerializer.Serialize(articlesData)}";
    }

    protected BaseTranslationService(IMemoryCache cache, ILogger logger)
    {
        Cache = cache;
        Logger = logger;
    }


    public async Task<IEnumerable<NewsArticle>> TranslateArticlesAsync(IEnumerable<NewsArticle> articles)
    {
        var articlesList = articles.ToList();
        if (articlesList.Count == 0) return articlesList;

        var resultList = new List<NewsArticle>();
        var toTranslate = new List<NewsArticle>();

        // 1. Verifica Cache
        foreach (var article in articlesList)
        {
            var cacheKey = $"trans_{article.Url.GetHashCode(StringComparison.CurrentCulture)}";
            if (Cache.TryGetValue(cacheKey, out NewsArticle? cachedArticle) && cachedArticle != null)
            {
                resultList.Add(cachedArticle);
            }
            else
            {
                toTranslate.Add(article);
            }
        }

        if (toTranslate.Count == 0) return resultList.Concat(articlesList.Where(a => !resultList.Any(r => r.Url == a.Url)));

        try
        {
            // 2. Chamada à API externa (implementada pelas subclasses)
            var translatedBatch = await CallExternalApiAsync(toTranslate).ConfigureAwait(false);
            
            foreach (var translated in translatedBatch)
            {
                var cacheKey = $"trans_{translated.Url.GetHashCode(StringComparison.CurrentCulture)}";
                Cache.Set(cacheKey, translated, TimeSpan.FromHours(1));
                resultList.Add(translated);
            }
        }
        catch (BaseInfrastructureException)
        {
            // Repassa para o orquestrador tratar (failover)
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Service}: Falha inesperada na tradução.", ServiceName);
            throw new TranslationException("Falha inesperada no serviço de tradução.", ServiceName, ex);
        }

        // Garante que retornamos na mesma ordem ou pelo menos todos os itens
        return articlesList.Select(orig => resultList.FirstOrDefault(r => r.Url == orig.Url) ?? orig);
    }

    protected abstract Task<IEnumerable<NewsArticle>> CallExternalApiAsync(List<NewsArticle> articles);

    protected BulkTranslationResponse CleanAndDeserializeResponse(string? jsonText)
    {
        if (string.IsNullOrEmpty(jsonText)) 
            throw new TranslationException("Resposta vazia ou inválida da API.", ServiceName);

        // Limpeza de Markdown
        var cleaned = jsonText.Trim();
        if (cleaned.StartsWith("```json")) cleaned = cleaned.Replace("```json", "");
        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);

        try 
        {
            return JsonSerializer.Deserialize<BulkTranslationResponse>(cleaned.Trim()) 
                   ?? throw new TranslationException("JSON nulo após desserialização.", ServiceName);
        }
        catch (JsonException ex)
        {
            throw new TranslationException("Falha ao desserializar resposta de tradução.", ServiceName, ex);
        }
    }

    protected List<NewsArticle> MapToDomain(List<NewsArticle> originalArticles, BulkTranslationResponse bulkDto)
    {
        var results = new List<NewsArticle>();
        foreach (var item in bulkDto.Translations)
        {
            var orig = originalArticles.FirstOrDefault(a => a.Url == item.Url);
            if (orig != null)
            {
                results.Add(new NewsArticle
                {
                    Id = orig.Id,
                    Url = orig.Url,
                    PublishedAt = orig.PublishedAt,
                    Title = item.Title,
                    Description = item.Summary
                });
            }
        }
        return results;
    }
}
