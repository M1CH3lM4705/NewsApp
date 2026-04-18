using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;

namespace NewsApp.Web.Components.UI;

public partial class NewsCard
{
    [Inject] 
    private INewsStateManager StateManager { get; set; } = null!;

    [Inject] 
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter] 
    public NewsArticle Article { get; set; } = null!;

    [Parameter] 
    public EventCallback<Guid> OnRead { get; set; }

    private bool _isRead = false;

    private async Task HandleAccess()
    {
        // 1. Marca como lida no estado (Servidor)
        StateManager.MarkAsRead(Article.Id);

        // 2. Dispara animação visual local
        _isRead = true;
        
        // 3. Abre a URL original de forma segura em nova aba
        await JSRuntime.InvokeVoidAsync("open", Article.Url, "_blank");

        // 4. Notifica o pai para remover o item da coleção após a animação
        await OnRead.InvokeAsync(Article.Id);
    }
}
