using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NewsApp.Application.Configuration;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.Services;
using System.Net;
using Xunit;

namespace NewsApp.Tests.Infrastructure;

public class TranslationOrchestratorTests
{
    private readonly Mock<HttpMessageHandler> _geminiHandler;
    private readonly Mock<HttpMessageHandler> _openRouterHandler;
    private readonly GeminiTranslationService _geminiService;
    private readonly OpenRouterTranslationService _openRouterService;
    private readonly Mock<ILogger<TranslationOrchestrator>> _mockOrchestratorLogger;
    private readonly TranslationOrchestrator _orchestrator;

    public TranslationOrchestratorTests()
    {
        _geminiHandler = new Mock<HttpMessageHandler>();
        _openRouterHandler = new Mock<HttpMessageHandler>();
        
        var geminiClient = new HttpClient(_geminiHandler.Object) { BaseAddress = new Uri("https://gemini.com/") };
        var openRouterClient = new HttpClient(_openRouterHandler.Object) { BaseAddress = new Uri("https://openrouter.ai/") };
        
        var config = Options.Create(new AppConfiguration 
        { 
            GeminiApiKey = "g-key", 
            OpenRouterApiKey = "or-key" 
        });
        
        var cache = new MemoryCache(new MemoryCacheOptions());
        var geminiLogger = new Mock<ILogger<GeminiTranslationService>>();
        var orLogger = new Mock<ILogger<OpenRouterTranslationService>>();
        
        _geminiService = new GeminiTranslationService(geminiClient, config, cache, geminiLogger.Object);
        _openRouterService = new OpenRouterTranslationService(openRouterClient, config, cache, orLogger.Object);
        
        _mockOrchestratorLogger = new Mock<ILogger<TranslationOrchestrator>>();
        _orchestrator = new TranslationOrchestrator(_geminiService, _openRouterService, _mockOrchestratorLogger.Object);
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldFailoverToOpenRouter_WhenGeminiReturns429()
    {
        // Arrange
        var articles = new List<NewsArticle> { new NewsArticle { Title = "Title", Url = "url" } };

        // Gemini returns 429
        _geminiHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.TooManyRequests });

        // OpenRouter returns Success
        var orResponse = new { choices = new[] { new { message = new { content = "{\"translations\": [{\"url\": \"url\", \"title\": \"Trad\", \"summary\": \"Sum\"}]}" } } } };
        _openRouterHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage 
            { 
                StatusCode = HttpStatusCode.OK, 
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(orResponse)) 
            });

        // Act
        var result = await _orchestrator.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("Trad", result.First().Title);
        _geminiHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        _openRouterHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TranslateArticlesAsync_ShouldReturnOriginal_WhenBothFail()
    {
        // Arrange
        var articles = new List<NewsArticle> { new NewsArticle { Title = "Original", Url = "url" } };

        _geminiHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.TooManyRequests });

        _openRouterHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await _orchestrator.TranslateArticlesAsync(articles);

        // Assert
        Assert.Equal("Original", result.First().Title);
    }
}
