using NewsApp.Application.Services;
using NewsApp.Domain.Entities;
using Xunit;

namespace NewsApp.Tests.Services;

public class NewsStateManagerTests
{
    [Fact]
    public void MarkAsRead_ShouldAddArticleToCollection()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var article = new NewsArticle { Id = Guid.NewGuid(), Title = "Test" };

        // Act
        stateManager.MarkAsRead(article);

        // Assert
        Assert.True(stateManager.IsRead(article.Id));
        Assert.Contains(article.Id, stateManager.GetReadArticleIds());
        Assert.Contains(article, stateManager.GetReadArticles());
    }

    [Fact]
    public void IsRead_ShouldReturnFalse_WhenIdNotRead()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var id = Guid.NewGuid();

        // Act & Assert
        Assert.False(stateManager.IsRead(id));
    }

    [Fact]
    public void GetReadArticleIds_ShouldReturnAllReadIds()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var art1 = new NewsArticle { Id = Guid.NewGuid(), Title = "A" };
        var art2 = new NewsArticle { Id = Guid.NewGuid(), Title = "B" };

        // Act
        stateManager.MarkAsRead(art1);
        stateManager.MarkAsRead(art2);
        var result = stateManager.GetReadArticleIds();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(art1.Id, result);
        Assert.Contains(art2.Id, result);
    }
}
