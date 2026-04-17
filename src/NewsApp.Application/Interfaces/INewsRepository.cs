using NewsApp.Domain.Entities;

namespace NewsApp.Application.Interfaces;

public interface INewsRepository
{
    Task<IEnumerable<NewsArticle>> GetArticlesAsync();
}
