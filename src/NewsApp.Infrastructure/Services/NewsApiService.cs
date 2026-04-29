using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.NewsApi;

namespace NewsApp.Infrastructure.Services;

public class NewsApiService : INewsRepository
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly ILogger<NewsApiService> _logger;

    public NewsApiService(HttpClient httpClient, IOptions<AppConfiguration> config, ILogger<NewsApiService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<NewsArticle>> GetArticlesAsync(string? category = null, string? query = null, int page = 1, int pageSize = 10)
    {
        try
        {
            var apiKey = _config.NewsApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("News API Key is missing. Returning empty articles.");
                return Enumerable.Empty<NewsArticle>();
            }

            var requestUri = BuildRequestUri(apiKey, category, query, page, pageSize);
            
            var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new ExternalServiceException(
                    "Falha ao obter notícias da NewsAPI.", 
                    nameof(NewsApiService), 
                    (int)response.StatusCode, 
                    errorContent);
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<NewsApiResponse>().ConfigureAwait(false);
            _logger.LogInformation("NewsAPI retornou {Count} artigos brutos.", apiResponse?.Articles?.Count ?? 0);

            return ProcessAndMapResponse(apiResponse?.Articles);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Erro de rede ao conectar com NewsAPI.", nameof(NewsApiService), innerException: ex);
        }
        catch (BaseInfrastructureException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("Erro inesperado ao processar notícias.", nameof(NewsApiService), innerException: ex);
        }
    }

    private string BuildRequestUri(string apiKey, string? category, string? query, int page, int pageSize)
    {
        // Sanitização e Limites
        var sanitizedCategory = System.Net.WebUtility.HtmlEncode(category ?? string.Empty);
        var sanitizedQuery = System.Net.WebUtility.HtmlEncode(query ?? string.Empty);
        if (sanitizedQuery.Length > 100) sanitizedQuery = sanitizedQuery.Substring(0, 100);

        var uri = $"https://newsapi.org/v2/top-headlines?country=us&apiKey={apiKey}&page={page}&pageSize={pageSize}";
        
        if (!string.IsNullOrEmpty(sanitizedCategory))
            uri += $"&category={sanitizedCategory}";
        
        if (!string.IsNullOrEmpty(sanitizedQuery))
            uri += $"&q={Uri.EscapeDataString(sanitizedQuery)}";

        return uri;
    }

    private IEnumerable<NewsArticle> ProcessAndMapResponse(IEnumerable<NewsApiArticle>? apiArticles)
    {
        if (apiArticles == null)
            return Enumerable.Empty<NewsArticle>();

        return apiArticles
            .Where(a => !string.IsNullOrEmpty(a.Url))
            .GroupBy(a => a.Url) // Deduplicação por URL
            .Select(g => g.First())
            .Select(a => 
            {
                var url = a.Url!;
                // Geramos um GUID determinístico baseado no hash da URL para persistência de identidade
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
                
                return new NewsArticle
                {
                    Id = new Guid(hash),
                    Title = a.Title ?? "Sem Título",
                    Description = a.Description ?? string.Empty,
                    Url = url,
                    PublishedAt = a.PublishedAt
                };
            });
    }
}
