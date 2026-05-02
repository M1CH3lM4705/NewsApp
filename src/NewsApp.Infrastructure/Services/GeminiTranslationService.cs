using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.Gemini;

namespace NewsApp.Infrastructure.Services;

public class GeminiTranslationService : BaseTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;

    protected override string ServiceName => nameof(GeminiTranslationService);

    public GeminiTranslationService(
        HttpClient httpClient, 
        IOptions<AppConfiguration> config, 
        IMemoryCache cache, 
        ILogger<GeminiTranslationService> logger) : base(cache, logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NewsAppMobile/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _config = config.Value;
    }

    protected override async Task<IEnumerable<NewsArticle>> CallExternalApiAsync(List<NewsArticle> articles)
    {
        if (string.IsNullOrEmpty(_config.GeminiApiKey))
            throw new InvalidOperationException("Gemini API Key is missing.");

        var request = BuildTranslationRequest(articles);

        HttpResponseMessage response;
        try 
        {
            response = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/gemini-2.5-flash-lite:generateContent?key={_config.GeminiApiKey}",
                request).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Erro de rede ao conectar com Gemini AI.", ServiceName, innerException: ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ExternalServiceException("Timeout ao conectar com Gemini AI.", ServiceName, innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new ExternalServiceException(
                $"Erro na API do Gemini: {response.StatusCode}", 
                ServiceName, 
                (int)response.StatusCode, 
                error);
        }

        var geminiResult = await response.Content.ReadFromJsonAsync<GeminiResponse>().ConfigureAwait(false);
        var jsonText = geminiResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        var bulkDto = CleanAndDeserializeResponse(jsonText);
        
        return MapToDomain(articles, bulkDto);
    }

    private GeminiRequest BuildTranslationRequest(List<NewsArticle> articles)
    {
        var request = new GeminiRequest();
        request.Contents.Add(new Content
        {
            Parts = new List<Part> { new Part { Text = $"{SystemPrompt}\n\n{GetUserPrompt(articles)}" } }
        });

        return request;
    }
}
