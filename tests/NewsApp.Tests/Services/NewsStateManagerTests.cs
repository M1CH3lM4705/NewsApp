using NewsApp.Application.Services;
using Xunit;

namespace NewsApp.Tests.Services;

public class NewsStateManagerTests
{
    [Fact]
    public void MarkAsRead_ShouldAddIdToCollection()
    {
        // Arrange
        var stateManager = new NewsStateManager();
        var id = Guid.NewGuid();

        // Act
        stateManager.MarkAsRead(id);

        // Assert
        Assert.True(stateManager.IsRead(id));
        Assert.Contains(id, stateManager.GetReadArticleIds());
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
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Act
        stateManager.MarkAsRead(id1);
        stateManager.MarkAsRead(id2);
        var result = stateManager.GetReadArticleIds();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(id1, result);
        Assert.Contains(id2, result);
    }
}
