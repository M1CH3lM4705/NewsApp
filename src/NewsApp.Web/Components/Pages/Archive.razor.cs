using Microsoft.AspNetCore.Components;
using MudBlazor;
using NewsApp.Application.Interfaces;
using NewsApp.Application.UseCases;
using NewsApp.Domain.Entities;

namespace NewsApp.Web.Components.Pages;

public partial class Archive
{
    [Inject] 
    private IGetLatestNewsUseCase NewsUseCase { get; set; } = null!;

    [Inject] 
    private INewsStateManager StateManager { get; set; } = null!;

    [Inject] 
    private ISnackbar Snackbar { get; set; } = null!;

    private List<NewsArticle> readArticles = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadArchive();
    }

    private async Task LoadArchive()
    {
        isLoading = true;
        // Buscamos todas as notícias e filtramos pelas que estão no StateManager
        var allArticles = await NewsUseCase.ExecuteAsync();
        var readIds = StateManager.GetReadArticleIds();
        
        readArticles = allArticles
            .Where(a => readIds.Contains(a.Id))
            .ToList();
            
        isLoading = false;
    }

    private void UnmarkAsRead(Guid id)
    {
        StateManager.MarkAsUnread(id);
        readArticles.RemoveAll(a => a.Id == id);
        Snackbar.Add("Notícia restaurada para o feed!", Severity.Success);
        StateHasChanged();
    }
}
