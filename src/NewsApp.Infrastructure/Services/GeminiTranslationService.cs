using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
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
        if (!articlesList.Any()) return articlesList;

        var resultList = new List<NewsArticle>();
        var toTranslate = new List<NewsArticle>();

        // 1. Verifica Cache
        foreach (var article in articlesList)
        {
            var cacheKey = $"trans_{article.Url.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out NewsArticle cachedArticle))
            {
                resultList.Add(cachedArticle);
            }
            else
            {
                toTranslate.Add(article);
            }
        }

        if (!toTranslate.Any()) return resultList.Concat(articlesList.Where(a => !resultList.Any(r => r.Url == a.Url)));

        try
        {
            // 2. Tradução em Massa (Bulk) para economizar cota
            var translatedBatch = await TranslateBatchWithGeminiAsync(toTranslate);
            
            foreach (var translated in translatedBatch)
            {
                var cacheKey = $"trans_{translated.Url.GetHashCode()}";
                _cache.Set(cacheKey, translated, TimeSpan.FromHours(1));
                resultList.Add(translated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback Global: Falha na tradução em massa. Mantendo originais.");
            return articlesList; // Retorna tudo original em caso de erro na API
        }

        // Garante que retornamos na mesma ordem ou pelo menos todos os itens
        return articlesList.Select(orig => resultList.FirstOrDefault(r => r.Url == orig.Url) ?? orig);
    }

    private async Task<IEnumerable<NewsArticle>> TranslateBatchWithGeminiAsync(List<NewsArticle> articles)
    {
        if (string.IsNullOrEmpty(_config.GeminiApiKey))
            throw new InvalidOperationException("Gemini API Key is missing.");

        var systemPrompt = "Você é um tradutor jornalístico. Traduza os títulos para português e gere resumos de no máximo 3 linhas em português. " +
                           "Receba uma lista de notícias e retorne um objeto JSON contendo um array chamado 'translations'. " +
                           "Cada item do array deve ter: 'url' (id único), 'title' e 'summary'.";

        var articlesJson = JsonSerializer.Serialize(articles.Select(a => new { a.Url, a.Title, a.Description }));
        
        var request = new GeminiRequest();
        request.Contents.Add(new Content
        {
            Parts = new List<Part> { new Part { Text = $"{systemPrompt}\n\nNotícias:\n{articlesJson}" } }
        });

        var response = await _httpClient.PostAsJsonAsync(
            $"v1beta/models/gemini-2.5-flash-lite:generateContent?key={_config.GeminiApiKey}", 
            request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode}. Details: {error}");
        }

        var geminiResult = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        var jsonText = geminiResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(jsonText)) throw new Exception("Empty response");

        // Limpeza de Markdown
        jsonText = jsonText.Trim();
        if (jsonText.StartsWith("```json")) jsonText = jsonText.Replace("```json", "");
        if (jsonText.EndsWith("```")) jsonText = jsonText.Substring(0, jsonText.Length - 3);

        var bulkDto = JsonSerializer.Deserialize<BulkTranslationResponse>(jsonText.Trim());
        
        var results = new List<NewsArticle>();
        foreach (var item in bulkDto?.Translations ?? new())
        {
            var orig = articles.FirstOrDefault(a => a.Url == item.Url);
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

public class BulkTranslationResponse
{
    [JsonPropertyName("translations")]
    public List<GeminiTranslationItem> Translations { get; set; } = new();
}

public class GeminiTranslationItem
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
