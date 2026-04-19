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

public class NewsCardTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Configura o MudBlazor para o bUnit
        Services.AddMudServices();

        // Mocks globais para o JS do MudBlazor evitar erros de "unhandled invocation"
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
        JSInterop.SetupVoid("mudPopover.connect", _ => true).SetVoidResult();
        
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void NewsCard_ShouldRenderCorrectContent()
    {
        // Arrange
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = "Test Title",
            Description = "Test Description",
            PublishedAt = new DateTime(2026, 4, 17),
            Url = "https://example.com"
        };
        var mockStateManager = new Mock<INewsStateManager>();
        Services.AddSingleton(mockStateManager.Object);

        // Act
        var cut = Render<NewsCard>(parameters => parameters
            .Add(p => p.Article, article)
        );

        // Assert
        Assert.Contains("Test Title", cut.Markup);
        Assert.Contains("Test Description", cut.Markup);
        Assert.Contains("17/04/2026", cut.Markup);
    }

    [Fact]
    public async Task HandleAccess_ShouldMarkAsReadAndInvokeJs()
    {
        // Arrange
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = "Test Title",
            Url = "https://example.com"
        };
        var mockStateManager = new Mock<INewsStateManager>();
        Services.AddSingleton(mockStateManager.Object);

        // Mock específico do botão abrir URL
        var jsOpenMock = JSInterop.SetupVoid("open", article.Url, "_blank");
        jsOpenMock.SetVoidResult();

        bool callbackInvoked = false;
        var cut = Render<NewsCard>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.OnRead, (Guid id) => { callbackInvoked = true; })
        );

        // Act
        var button = cut.Find("button"); // O botão "Ler Mais"
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        mockStateManager.Verify(m => m.MarkAsRead(It.Is<NewsArticle>(a => a.Id == article.Id)), Times.Once);
        this.JSInterop.VerifyInvoke("open", 1);
        
        // Aguarda a execução da callback usando WaitForState ou WaitForAssertion
        cut.WaitForState(() => callbackInvoked, TimeSpan.FromSeconds(2));
        Assert.True(callbackInvoked);
    }
}
