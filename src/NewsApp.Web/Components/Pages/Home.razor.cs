using Microsoft.AspNetCore.Components;
using NewsApp.Application.UseCases;
using NewsApp.Domain.Entities;

namespace NewsApp.Web.Components.Pages;

public partial class Home
{
    [Inject] 
    private IGetLatestNewsUseCase NewsUseCase { get; set; } = null!;

    private List<NewsArticle> _articles = new();
    private bool isSyncing = false;
    private string? _selectedCategory = "technology";
    private string? _searchQuery;
    private int _currentPage = 1;
    private const int PageSize = 10;

    protected override async Task OnInitializedAsync()
    {
        await SyncNews(reset: true);
    }

    private async Task OnCategoryChanged(string category)
    {
        _selectedCategory = category;
        await SyncNews(reset: true);
    }

    private async Task OnSearch()
    {
        await SyncNews(reset: true);
    }

    private async Task LoadMore()
    {
        _currentPage++;
        await SyncNews(reset: false);
    }

    private async Task SyncNews(bool reset)
    {
        if (reset)
        {
            _currentPage = 1;
            _articles.Clear();
        }

        isSyncing = true;
        // Simulando delay de rede para UX
        await Task.Delay(500);
        
        var newArticles = await NewsUseCase.ExecuteAsync(_selectedCategory, _searchQuery, _currentPage, PageSize);
        foreach (var article in newArticles)
        {
            if (!_articles.Any(a => a.Url == article.Url))
            {
                _articles.Add(article);
            }
        }
        
        isSyncing = false;
    }
}
