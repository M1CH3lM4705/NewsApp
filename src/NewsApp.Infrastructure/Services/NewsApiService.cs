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

    public async Task<IEnumerable<NewsArticle>> GetArticlesAsync()
    {
        try
        {
            var apiKey = _config.NewsApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("News API Key is missing.");
                return Enumerable.Empty<NewsArticle>();
            }

            // Exemplo consumindo top-headlines. Em produção a URL base estaria no appsettings/DI
            var requestUri = $"https://newsapi.org/v2/top-headlines?country=us&apiKey={apiKey}";
            
            var response = await _httpClient.GetAsync(requestUri);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("NewsAPI returned error: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<NewsArticle>();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<NewsApiResponse>();
            
            if (apiResponse?.Articles == null)
                return Enumerable.Empty<NewsArticle>();

            return apiResponse.Articles.Select(a => new NewsArticle
            {
                Id = Guid.NewGuid(),
                Title = a.Title ?? "No Title",
                Description = a.Description ?? string.Empty,
                Url = a.Url ?? string.Empty,
                PublishedAt = a.PublishedAt
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
