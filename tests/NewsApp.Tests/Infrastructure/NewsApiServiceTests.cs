using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NewsApp.Application.Configuration;
using NewsApp.Infrastructure.ExternalApis.NewsApi;
using NewsApp.Infrastructure.Services;
using Xunit;

namespace NewsApp.Tests.Infrastructure;

public class NewsApiServiceTests
{
    [Fact]
    public async Task GetArticlesAsync_ShouldReturnArticles_WhenApiCallIsSuccessful()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        var fakeApiResponse = new NewsApiResponse
        {
            Status = "ok",
            Articles = new List<NewsApiArticle>
            {
                new NewsApiArticle { Title = "Mock News 1", PublishedAt = DateTime.UtcNow },
                new NewsApiArticle { Title = "Mock News 2", PublishedAt = DateTime.UtcNow }
            }
        };

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(fakeApiResponse))
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var configOptions = Options.Create(new AppConfiguration { NewsApiKey = "fake-key" });
        var mockLogger = new Mock<ILogger<NewsApiService>>();

        var service = new NewsApiService(httpClient, configOptions, mockLogger.Object);

        // Act
        var result = await service.GetArticlesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, a => a.Title == "Mock News 1");
    }

    [Fact]
    public async Task GetArticlesAsync_ShouldReturnEmpty_WhenApiReturnsError()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var configOptions = Options.Create(new AppConfiguration { NewsApiKey = "invalid-key" });
        var mockLogger = new Mock<ILogger<NewsApiService>>();

        var service = new NewsApiService(httpClient, configOptions, mockLogger.Object);

        // Act
        var result = await service.GetArticlesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetArticlesAsync_ShouldReturnEmpty_WhenNetworkErrorOccurs()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var configOptions = Options.Create(new AppConfiguration { NewsApiKey = "key" });
        var mockLogger = new Mock<ILogger<NewsApiService>>();

        var service = new NewsApiService(httpClient, configOptions, mockLogger.Object);

        // Act
        var result = await service.GetArticlesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetArticlesAsync_ShouldReturnEmpty_WhenJsonIsMalformed()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid-json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var configOptions = Options.Create(new AppConfiguration { NewsApiKey = "key" });
        var mockLogger = new Mock<ILogger<NewsApiService>>();

        var service = new NewsApiService(httpClient, configOptions, mockLogger.Object);

        // Act
        var result = await service.GetArticlesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
