using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.Gemini;

namespace NewsApp.Infrastructure.Services;

public class GeminiTranslationService : IGeminiTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeminiTranslationService> _logger;

    public GeminiTranslationService(
        HttpClient httpClient, 
        IOptions<AppConfiguration> config, 
        IMemoryCache cache, 
        ILogger<GeminiTranslationService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _cache = cache;
        _logger = logger;
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
            if (_cache.TryGetValue(cacheKey, out NewsArticle? cachedArticle) && cachedArticle != null)
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
            // 2. Tradução em Massa (Bulk) para economizar cota
            var translatedBatch = await TranslateBatchWithGeminiAsync(toTranslate).ConfigureAwait(false);
            
            foreach (var translated in translatedBatch)
            {
                var cacheKey = $"trans_{translated.Url.GetHashCode(StringComparison.CurrentCulture)}";
                _cache.Set(cacheKey, translated, TimeSpan.FromHours(1));
                resultList.Add(translated);
            }
        }
        catch (BaseInfrastructureException ex)
        {
            _logger.LogWarning("Falha na infraestrutura de tradução: {Message}. Serviço: {Service}. Usando originais.", ex.Message, ex.ServiceName);
            return articlesList;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback Global: Falha inesperada na tradução. Mantendo originais.");
            return articlesList; 
        }

        // Garante que retornamos na mesma ordem ou pelo menos todos os itens
        return articlesList.Select(orig => resultList.FirstOrDefault(r => r.Url == orig.Url) ?? orig);
    }

    private async Task<IEnumerable<NewsArticle>> TranslateBatchWithGeminiAsync(List<NewsArticle> articles)
    {
        if (string.IsNullOrEmpty(_config.GeminiApiKey))
            throw new InvalidOperationException("Gemini API Key is missing.");

        var request = BuildTranslationRequest(articles);

        HttpResponseMessage response;
        try 
        {
            response = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/gemini-2.5-flash-lite:generateContent?key={_config.GeminiApiKey}",
                request).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Erro de rede ao conectar com Gemini AI.", nameof(GeminiTranslationService), innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new ExternalServiceException(
                $"Erro na API do Gemini: {response.StatusCode}", 
                nameof(GeminiTranslationService), 
                (int)response.StatusCode, 
                error);
        }

        var geminiResult = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        var jsonText = geminiResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        var bulkDto = CleanAndDeserializeResponse(jsonText);
        
        return MapToDomain(articles, bulkDto);
    }

    private GeminiRequest BuildTranslationRequest(List<NewsArticle> articles)
    {
        var systemPrompt = "Você é um tradutor jornalístico. Traduza os títulos para português e gere resumos de no máximo 3 linhas em português. " +
                           "Receba uma lista de notícias e retorne um objeto JSON contendo um array chamado 'translations'. " +
                           "Cada item do array deve ter: 'url' (id único), 'title' e 'summary'.";

        var articlesJson = JsonSerializer.Serialize(articles.Select(a => new { a.Url, a.Title, a.Description }));
        
        var request = new GeminiRequest();
        request.Contents.Add(new Content
        {
            Parts = new List<Part> { new Part { Text = $"{systemPrompt}\n\nNotícias:\n{articlesJson}" } }
        });

        return request;
    }

    private BulkTranslationResponse CleanAndDeserializeResponse(string? jsonText)
    {
        if (string.IsNullOrEmpty(jsonText)) 
            throw new TranslationException("Resposta vazia ou inválida do Gemini.", nameof(GeminiTranslationService));

        // Limpeza de Markdown
        var cleaned = jsonText.Trim();
        if (cleaned.StartsWith("```json")) cleaned = cleaned.Replace("```json", "");
        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);

        try 
        {
            return JsonSerializer.Deserialize<BulkTranslationResponse>(cleaned.Trim()) 
                   ?? throw new TranslationException("JSON nulo após desserialização.", nameof(GeminiTranslationService));
        }
        catch (JsonException ex)
        {
            throw new TranslationException("Falha ao desserializar resposta de tradução do Gemini.", nameof(GeminiTranslationService), ex);
        }
    }

    private List<NewsArticle> MapToDomain(List<NewsArticle> originalArticles, BulkTranslationResponse bulkDto)
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
