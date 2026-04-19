using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApp.Application.Configuration;
using NewsApp.Domain.Entities;
using NewsApp.Domain.Exceptions;
using NewsApp.Infrastructure.ExternalApis.OpenRouter;

namespace NewsApp.Infrastructure.Services;

public class OpenRouterTranslationService : BaseTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;

    protected override string ServiceName => nameof(OpenRouterTranslationService);

    public OpenRouterTranslationService(
        HttpClient httpClient, 
        IOptions<AppConfiguration> config, 
        IMemoryCache cache, 
        ILogger<OpenRouterTranslationService> logger) : base(cache, logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    protected override async Task<IEnumerable<NewsArticle>> CallExternalApiAsync(List<NewsArticle> articles)
    {
        if (string.IsNullOrEmpty(_config.OpenRouterApiKey))
            throw new InvalidOperationException("OpenRouter API Key is missing.");

        var request = BuildTranslationRequest(articles);

        HttpResponseMessage response;
        try 
        {
            // O HttpClient injetado já deve ter os headers base se configurado via IHttpClientFactory no Program.cs
            // Mas para garantir, podemos adicionar aqui ou confiar na configuração do Program.cs
            response = await _httpClient.PostAsJsonAsync(
                "api/v1/chat/completions",
                request).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Erro de rede ao conectar com OpenRouter.", ServiceName, innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new ExternalServiceException(
                $"Erro na API do OpenRouter: {response.StatusCode}", 
                ServiceName, 
                (int)response.StatusCode, 
                error);
        }

        var openRouterResult = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
        var jsonText = openRouterResult?.Choices?.FirstOrDefault()?.Message?.Content;

        var bulkDto = CleanAndDeserializeResponse(jsonText);
        
        return MapToDomain(articles, bulkDto);
    }

    private OpenRouterRequest BuildTranslationRequest(List<NewsArticle> articles)
    {
        return new OpenRouterRequest
        {
            Messages = new List<OpenRouterMessage>
            {
                new OpenRouterMessage { Role = "system", Content = SystemPrompt },
                new OpenRouterMessage { Role = "user", Content = GetUserPrompt(articles) }
            }
        };
    }
}
