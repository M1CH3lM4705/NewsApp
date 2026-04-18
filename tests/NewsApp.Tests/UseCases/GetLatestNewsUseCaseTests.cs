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

        mockRepository.Setup(repo => repo.GetArticlesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(articles);

        var mockGemini = new Mock<IGeminiTranslationService>();
        mockGemini.Setup(g => g.TranslateArticlesAsync(It.IsAny<IEnumerable<NewsArticle>>()))
            .ReturnsAsync((IEnumerable<NewsArticle> input) => input);

        var useCase = new GetLatestNewsUseCase(mockRepository.Object, mockGemini.Object);

        // Act
        var result = await useCase.ExecuteAsync(null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        // O UseCase agora retorna hoje e ontem, então as 3 notícias devem vir (2 de hoje, 1 de ontem)
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFromCache_OnSecondCall()
    {
        // Arrange
        var mockRepository = new Mock<INewsRepository>();
        var mockGemini = new Mock<IGeminiTranslationService>();
        
        var articles = new List<NewsArticle> { new NewsArticle { Title = "Cache Test", PublishedAt = DateTime.UtcNow } };
        
        // Setup repository to return data only once
        mockRepository.Setup(repo => repo.GetArticlesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(articles);
        
        mockGemini.Setup(g => g.TranslateArticlesAsync(It.IsAny<IEnumerable<NewsArticle>>()))
            .ReturnsAsync((IEnumerable<NewsArticle> input) => input);

        var useCase = new GetLatestNewsUseCase(mockRepository.Object, mockGemini.Object);
        var category = "unique-cache-category-" + Guid.NewGuid();

        // Act
        // Primeira chamada: busca no repositório
        var result1 = await useCase.ExecuteAsync(category, null, 1, 10);
        // Segunda chamada: deve retornar do cache
        var result2 = await useCase.ExecuteAsync(category, null, 1, 10);

        // Assert
        Assert.Equal(result1, result2);
        // Verifica que o repositório foi chamado apenas UMA VEZ
        mockRepository.Verify(repo => repo.GetArticlesAsync(category, null, 1, 10), Times.Once);
        mockGemini.Verify(g => g.TranslateArticlesAsync(It.IsAny<IEnumerable<NewsArticle>>()), Times.Once);
    }
}
