using Microsoft.AspNetCore.Components;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;

namespace NewsApp.Web.Components.UI;

public partial class NewsList
{
    [Inject] 
    private INewsStateManager StateManager { get; set; } = null!;

    [Parameter] 
    public IEnumerable<NewsArticle> Articles { get; set; } = Enumerable.Empty<NewsArticle>();

    private List<NewsArticle> _filteredArticles = new();

    protected override void OnParametersSet()
    {
        // Sempre que os parâmetros mudarem, re-calculamos a lista
        UpdateFilteredArticles();
    }

    private void UpdateFilteredArticles()
    {
        _filteredArticles = Articles.Where(a => !StateManager.IsRead(a.Id)).ToList();
    }

    private async Task RefreshList(Guid id)
    {
        // 1. Aguarda o fade-out visual
        await Task.Delay(450); 
        
        // 2. Remove o item especificamente da lista local para garantir que o Blazor entenda a mudança de coleção
        _filteredArticles.RemoveAll(a => a.Id == id);
        
        // 3. Força a re-renderização
        await InvokeAsync(StateHasChanged);
    }
}
