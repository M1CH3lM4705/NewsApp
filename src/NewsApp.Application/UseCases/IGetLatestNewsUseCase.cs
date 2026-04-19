using NewsApp.Domain.Entities;

namespace NewsApp.Application.UseCases;

public interface IGetLatestNewsUseCase
{
    Task<IEnumerable<NewsArticle>> ExecuteAsync(string? category = null, string? query = null, int page = 1, int pageSize = 10);
}
