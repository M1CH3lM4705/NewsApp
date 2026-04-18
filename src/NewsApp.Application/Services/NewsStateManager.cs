using NewsApp.Application.Interfaces;

namespace NewsApp.Application.Services;

public class NewsStateManager : INewsStateManager
{
    private readonly HashSet<Guid> _readArticles = new();

    public HashSet<Guid> GetReadArticleIds()
    {
        return _readArticles;
    }

    public void MarkAsRead(Guid articleId)
    {
        _readArticles.Add(articleId);
    }

    public void MarkAsUnread(Guid articleId)
    {
        _readArticles.Remove(articleId);
    }

    public bool IsRead(Guid articleId)
    {
        return _readArticles.Contains(articleId);
    }
}
