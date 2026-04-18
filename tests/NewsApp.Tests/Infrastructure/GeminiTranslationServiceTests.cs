using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NewsApp.Application.Configuration;
using NewsApp.Domain.Entities;
using NewsApp.Infrastructure.ExternalApis.Gemini;
using NewsApp.Infrastructure.Services;
using Xunit;

namespace NewsApp.Tests.Infrastructure;

public class GeminiTranslationServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<GeminiTranslationService>> _mockLogger;
    private readonly IOptions<AppConfiguration> _config;

    public GeminiTranslationServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<GeminiTranslationService>>();
        _config = Options.Create(new AppConfiguration { GeminiApiKey = "valid-key" });
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldTranslateBulk_WhenApiCallIsSuccessful()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "Title 1", Url = "url1" },
            new NewsArticle { Title = "Title 2", Url = "url2" }
        };

        var translationResult = new BulkTranslationResponse
        {
            Translations = new List<GeminiTranslationItem>
            {
                new GeminiTranslationItem { Url = "url1", Title = "T1 Trad", Summary = "S1 Trad" },
                new GeminiTranslationItem { Url = "url2", Title = "T2 Trad", Summary = "S2 Trad" }
            }
        };

        SetupMockResponse(JsonSerializer.Serialize(translationResult));

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal("T1 Trad", result.First(a => a.Url == "url1").Title);
        Assert.Equal("T2 Trad", result.First(a => a.Url == "url2").Title);
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldFallbackToOriginal_WhenGeminiOmitsArticle()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "Original 1", Url = "url1" },
            new NewsArticle { Title = "Original 2", Url = "url2" }
        };

        // Gemini only returns 1 translation
        var translationResult = new BulkTranslationResponse
        {
            Translations = new List<GeminiTranslationItem>
            {
                new GeminiTranslationItem { Url = "url1", Title = "T1 Trad", Summary = "S1 Trad" }
            }
        };

        SetupMockResponse(JsonSerializer.Serialize(translationResult));

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("T1 Trad", result.First(a => a.Url == "url1").Title);
        Assert.Equal("Original 2", result.First(a => a.Url == "url2").Title); // Fallback to original
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldReturnOriginals_WhenApiFails()
    {
        // Arrange
        var articles = new List<NewsArticle> { new NewsArticle { Title = "Original", Url = "url" } };
        
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.TooManyRequests });

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("Original", result.First().Title);
    }

    private void SetupMockResponse(string jsonText)
    {
        var geminiResponse = new GeminiResponse
        {
            Candidates = new List<Candidate>
            {
                new Candidate { Content = new Content { Parts = new List<Part> { new Part { Text = jsonText } } } }
            }
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponse))
            });
    }
}
