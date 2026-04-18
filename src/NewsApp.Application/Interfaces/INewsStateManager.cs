namespace NewsApp.Application.Interfaces;

using NewsApp.Domain.Entities;

public interface INewsStateManager
{
    event Action OnStateChanged;
    IEnumerable<NewsArticle> GetReadArticles();
    HashSet<Guid> GetReadArticleIds();
    void MarkAsRead(NewsArticle article);
    void MarkAsUnread(Guid articleId);
    bool IsRead(Guid articleId);
}
