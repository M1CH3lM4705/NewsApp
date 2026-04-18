namespace NewsApp.Application.Interfaces;

public interface INewsStateManager
{
    HashSet<Guid> GetReadArticleIds();
    void MarkAsRead(Guid articleId);
    void MarkAsUnread(Guid articleId);
    bool IsRead(Guid articleId);
}
