using NewsApp.Application.Services;
using NewsApp.Domain.Entities;
using Xunit;

namespace NewsApp.Tests.Services;

public class NewsStateManagerTests
{
    [Fact]
    public void MarkAsRead_ShouldAddArticle_AndTriggerEvent()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var article = new NewsArticle { Id = Guid.NewGuid(), Title = "Test" };
        bool eventTriggered = false;
        stateManager.OnStateChanged += () => eventTriggered = true;

        // Act
        stateManager.MarkAsRead(article);

        // Assert
        Assert.True(stateManager.IsRead(article.Id));
        Assert.True(eventTriggered);
    }

    [Fact]
    public void MarkAsRead_ShouldNotTriggerEvent_IfAlreadyRead()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var article = new NewsArticle { Id = Guid.NewGuid(), Title = "Test" };
        stateManager.MarkAsRead(article);
        
        int triggerCount = 0;
        stateManager.OnStateChanged += () => triggerCount++;

        // Act
        stateManager.MarkAsRead(article);

        // Assert
        Assert.Equal(0, triggerCount);
    }

    [Fact]
    public void MarkAsUnread_ShouldRemoveArticle_AndTriggerEvent()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var id = Guid.NewGuid();
        stateManager.MarkAsRead(new NewsArticle { Id = id });
        
        bool eventTriggered = false;
        stateManager.OnStateChanged += () => eventTriggered = true;

        // Act
        stateManager.MarkAsUnread(id);

        // Assert
        Assert.False(stateManager.IsRead(id));
        Assert.True(eventTriggered);
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
