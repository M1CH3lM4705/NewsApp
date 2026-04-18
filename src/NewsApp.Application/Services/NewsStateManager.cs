using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;

namespace NewsApp.Application.Services;

public class NewsStateManager : INewsStateManager
{
    private readonly Dictionary<Guid, NewsArticle> _readArticles = new();

    public event Action? OnStateChanged;

    public IEnumerable<NewsArticle> GetReadArticles()
    {
        return _readArticles.Values.OrderByDescending(a => a.PublishedAt);
    }

    public HashSet<Guid> GetReadArticleIds()
    {
        return _readArticles.Keys.ToHashSet();
    }

    public void MarkAsRead(NewsArticle article)
    {
        if (!_readArticles.ContainsKey(article.Id))
        {
            _readArticles.Add(article.Id, article);
            OnStateChanged?.Invoke();
        }
    }

    public void MarkAsUnread(Guid articleId)
    {
        if (_readArticles.Remove(articleId))
        {
            OnStateChanged?.Invoke();
        }
    }

    public bool IsRead(Guid articleId)
    {
        return _readArticles.ContainsKey(articleId);
    }
}
