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
        try 
        {
            Console.WriteLine("DEBUG: Archive - LoadArchive started");
            isLoading = true;
            await Task.Delay(200);
            
            Console.WriteLine("DEBUG: Archive - Fetching read articles");
            readArticles = StateManager.GetReadArticles().ToList();
            
            Console.WriteLine($"DEBUG: Archive - Found {readArticles.Count} articles");
            isLoading = false;
            Console.WriteLine("DEBUG: Archive - LoadArchive finished");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Archive ERROR - {ex.Message}");
            isLoading = false;
        }
    }

    private void UnmarkAsRead(Guid id)
    {
        StateManager.MarkAsUnread(id);
        readArticles.RemoveAll(a => a.Id == id);
        Snackbar.Add("Notícia restaurada para o feed!", Severity.Success);
        StateHasChanged();
    }
}
