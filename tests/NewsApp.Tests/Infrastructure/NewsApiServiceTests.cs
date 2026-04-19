using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NewsApp.Application.Configuration;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.NewsApi;
using NewsApp.Infrastructure.Services;
using Xunit;

namespace NewsApp.Tests.Infrastructure;

public class NewsApiServiceTests
{
    [Fact]
    public async Task GetArticlesAsync_ShouldReturnArticles_AndDeduplicateByUrl()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        var fakeApiResponse = new NewsApiResponse
        {
            Status = "ok",
            Articles = new List<NewsApiArticle>
            {
                new NewsApiArticle { Title = "News 1", Url = "https://news1.com", PublishedAt = DateTime.UtcNow },
                new NewsApiArticle { Title = "News 1 Duplicate", Url = "https://news1.com", PublishedAt = DateTime.UtcNow },
                new NewsApiArticle { Title = "News 2", Url = "https://news2.com", PublishedAt = DateTime.UtcNow }
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
        Assert.Equal(2, result.Count()); // Deduplicated
        Assert.Single(result, a => a.Url == "https://news1.com");
    }

    [Fact]
    public async Task GetArticlesAsync_ShouldThrowExternalServiceException_WhenApiReturnsError()
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => service.GetArticlesAsync());
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(nameof(NewsApiService), ex.ServiceName);
    }

    [Fact]
    public async Task GetArticlesAsync_ShouldThrowExternalServiceException_WhenNetworkErrorOccurs()
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => service.GetArticlesAsync());
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }
}
