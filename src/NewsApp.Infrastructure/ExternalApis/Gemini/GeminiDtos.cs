using System.Text.Json.Serialization;

namespace NewsApp.Infrastructure.ExternalApis.Gemini;

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<Content> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GenerationConfig GenerationConfig { get; set; } = new() { ResponseMimeType = "application/json" };
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();
}

public class Part
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class GenerationConfig
{
    [JsonPropertyName("response_mime_type")]
    public string ResponseMimeType { get; set; } = "application/json";
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate> Candidates { get; set; } = new();
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content Content { get; set; } = new();
}

// DTO para a estrutura JSON retornada pelo Gemini dentro do campo "text"
public class TranslatedArticleDto
{
    [JsonPropertyName("translatedTitle")]
    public string TranslatedTitle { get; set; } = string.Empty;

    [JsonPropertyName("shortSummary")]
    public string ShortSummary { get; set; } = string.Empty;
}
