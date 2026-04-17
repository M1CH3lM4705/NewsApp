using Moq;
using NewsApp.Application.Interfaces;
using NewsApp.Application.UseCases;
using NewsApp.Domain.Entities;
using Xunit;

namespace NewsApp.Tests.UseCases;

public class GetLatestNewsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyArticlesPublishedToday()
    {
        // Arrange
        var mockRepository = new Mock<INewsRepository>();
        var today = DateTime.UtcNow.Date;

        var articles = new List<NewsArticle>
        {
            new NewsArticle { Title = "News 1", PublishedAt = today.AddHours(2) },
            new NewsArticle { Title = "News 2", PublishedAt = today.AddDays(-1) },
            new NewsArticle { Title = "News 3", PublishedAt = today.AddHours(10) }
        };

        mockRepository.Setup(repo => repo.GetArticlesAsync())
            .ReturnsAsync(articles);

        // GetLatestNewsUseCase não existe ainda como classe concreta, então o teste não compilará.
        // Isso é o "Red" do TDD.
        var useCase = new GetLatestNewsUseCase(mockRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, article => Assert.Equal(today, article.PublishedAt.Date));
    }
}
