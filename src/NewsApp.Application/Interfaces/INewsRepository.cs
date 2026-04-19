using NewsApp.Domain.Entities;

namespace NewsApp.Application.Interfaces;

public interface INewsRepository
{
    Task<IEnumerable<NewsArticle>> GetArticlesAsync(string? category = null, string? query = null, int page = 1, int pageSize = 10);
}
