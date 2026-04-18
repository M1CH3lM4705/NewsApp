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
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("https://gemini.api/") };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<GeminiTranslationService>>();
        _config = Options.Create(new AppConfiguration { GeminiApiKey = "valid-key" });
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldTranslateArticles_WhenApiCallIsSuccessful()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "English Title", Description = "English Desc", Url = "http://news.com/1" } 
        };

        var geminiResponse = new GeminiResponse
        {
            Candidates = new List<Candidate>
            {
                new Candidate
                {
                    Content = new Content
                    {
                        Parts = new List<Part> { new Part { Text = "{\"translatedTitle\": \"Título Traduzido\", \"shortSummary\": \"Resumo Traduzido\"}" } }
                    }
                }
            }
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponse))
            });

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        var translated = result.First();
        Assert.Equal("Título Traduzido", translated.Title);
        Assert.Equal("Resumo Traduzido", translated.Description);
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldReturnOriginal_WhenApiFails()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "Original Title", Url = "http://news.com/2" } 
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("Original Title", result.First().Title);
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldUseCache_OnSubsequentCalls()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "English Title", Url = "http://news.com/3" } 
        };

        var geminiResponse = new GeminiResponse
        {
            Candidates = new List<Candidate>
            {
                new Candidate
                {
                    Content = new Content
                    {
                        Parts = new List<Part> { new Part { Text = "{\"translatedTitle\": \"Cached Title\", \"shortSummary\": \"...\"}" } }
                    }
                }
            }
        };

        _mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponse))
            })
            .ThrowsAsync(new Exception("Should not be called twice"));

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        await service.TranslateArticlesAsync(articles); // First call (fills cache)
        var result = await service.TranslateArticlesAsync(articles); // Second call (from cache)

        // Assert
        Assert.Equal("Cached Title", result.First().Title);
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldHandleMalformedJsonFromGemini()
    {
        // Arrange
        var articles = new List<NewsArticle> 
        { 
            new NewsArticle { Title = "Original Title", Url = "http://news.com/4" } 
        };

        var geminiResponse = new GeminiResponse
        {
            Candidates = new List<Candidate>
            {
                new Candidate
                {
                    Content = new Content
                    {
                        Parts = new List<Part> { new Part { Text = "not-a-json" } }
                    }
                }
            }
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponse))
            });

        var service = new GeminiTranslationService(_httpClient, _config, _cache, _mockLogger.Object);

        // Act
        var result = await service.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("Original Title", result.First().Title);
    }
}
