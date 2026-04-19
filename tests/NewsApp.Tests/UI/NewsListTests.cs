using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;
using NewsApp.Web.Components.UI;
using Xunit;

namespace NewsApp.Tests.UI;

public class NewsListTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        Services.AddMudServices();

        // Mocks globais para o JS do MudBlazor evitar erros de "unhandled invocation"
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void NewsList_ShouldShowEmptyState_WhenNoArticles()
    {
        // Arrange
        var mockStateManager = new Mock<INewsStateManager>();
        Services.AddSingleton(mockStateManager.Object);

        // Act
        var cut = Render<NewsList>(parameters => parameters
            .Add(p => p.Articles, Enumerable.Empty<NewsArticle>())
        );

        // Assert
        Assert.Contains("Nenhuma notícia nova no momento", cut.Markup);
    }

    [Fact]
    public void NewsList_ShouldRenderOnlyUnreadArticles()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var articles = new List<NewsArticle>
        {
            new NewsArticle { Id = id1, Title = "Unread News" },
            new NewsArticle { Id = id2, Title = "Read News" }
        };

        var mockStateManager = new Mock<INewsStateManager>();
        // Mocking id2 as read
        mockStateManager.Setup(m => m.IsRead(id1)).Returns(false);
        mockStateManager.Setup(m => m.IsRead(id2)).Returns(true);
        Services.AddSingleton(mockStateManager.Object);

        // Act
        var cut = Render<NewsList>(parameters => parameters
            .Add(p => p.Articles, articles)
        );

        // Assert
        // Verifica que o card da notícia não lida está presente
        Assert.Contains("Unread News", cut.Markup);
        // Verifica que o card da notícia lida NÃO está presente
        Assert.DoesNotContain("Read News", cut.Markup);
    }
}
