using System.Text.Json.Serialization;

namespace NewsApp.Infrastructure.ExternalApis.NewsApi;

public class NewsApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("articles")]
    public List<NewsApiArticle> Articles { get; set; } = new();
}

public class NewsApiArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }
}
