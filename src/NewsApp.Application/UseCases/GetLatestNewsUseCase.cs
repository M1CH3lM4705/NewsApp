using NewsApp.Application.Interfaces;
using NewsApp.Domain.Entities;

namespace NewsApp.Application.UseCases;

public class GetLatestNewsUseCase : IGetLatestNewsUseCase
{
    private readonly INewsRepository _newsRepository;

    public GetLatestNewsUseCase(INewsRepository newsRepository)
    {
        _newsRepository = newsRepository;
    }

    public async Task<IEnumerable<NewsArticle>> ExecuteAsync()
    {
        var articles = await _newsRepository.GetArticlesAsync();
        var today = DateTime.UtcNow.Date;

        return articles.Where(article => article.PublishedAt.Date == today);
    }
}
