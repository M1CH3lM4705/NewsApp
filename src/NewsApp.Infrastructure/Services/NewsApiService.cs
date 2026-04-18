using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
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
                _logger.LogWarning("News API Key is missing.");
                return Enumerable.Empty<NewsArticle>();
            }

            // Sanitização de inputs
            var sanitizedCategory = System.Net.WebUtility.HtmlEncode(category ?? string.Empty);
            var sanitizedQuery = System.Net.WebUtility.HtmlEncode(query ?? string.Empty);
            if (sanitizedQuery.Length > 100) sanitizedQuery = sanitizedQuery.Substring(0, 100);

            var requestUri = $"https://newsapi.org/v2/top-headlines?country=us&apiKey={apiKey}&page={page}&pageSize={pageSize}";
            
            if (!string.IsNullOrEmpty(sanitizedCategory))
                requestUri += $"&category={sanitizedCategory}";
            
            if (!string.IsNullOrEmpty(sanitizedQuery))
                requestUri += $"&q={Uri.EscapeDataString(sanitizedQuery)}";
            
            var response = await _httpClient.GetAsync(requestUri);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("NewsAPI returned error: {StatusCode}. Details: {ErrorContent}", response.StatusCode, errorContent);
                return Enumerable.Empty<NewsArticle>();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<NewsApiResponse>();
            
            _logger.LogInformation("NewsAPI returned {Count} articles.", apiResponse?.Articles?.Count ?? 0);

            if (apiResponse?.Articles == null)
                return Enumerable.Empty<NewsArticle>();

            return apiResponse.Articles
                .Where(a => !string.IsNullOrEmpty(a.Url))
                .GroupBy(a => a.Url)
                .Select(g => g.First())
                .Select(a => {
                var url = a.Url!;
                // Geramos um GUID determinístico baseado no hash da URL
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
                var guid = new Guid(hash);

                return new NewsArticle
                {
                    Id = guid,
                    Title = a.Title ?? "No Title",
                    Description = a.Description ?? string.Empty,
                    Url = url,
                    PublishedAt = a.PublishedAt
                };
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while reaching NewsAPI.");
            return Enumerable.Empty<NewsArticle>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in NewsApiService.");
            return Enumerable.Empty<NewsArticle>();
        }
    }
}
