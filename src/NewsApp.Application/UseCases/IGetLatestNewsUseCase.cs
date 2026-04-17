using NewsApp.Domain.Entities;

namespace NewsApp.Application.UseCases;

public interface IGetLatestNewsUseCase
{
    Task<IEnumerable<NewsArticle>> ExecuteAsync();
}
