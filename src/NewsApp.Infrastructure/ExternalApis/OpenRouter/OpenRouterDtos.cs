using System.Text.Json.Serialization;

namespace NewsApp.Infrastructure.ExternalApis.OpenRouter;

public class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "qwen/qwen3-coder:free";

    [JsonPropertyName("messages")]
    public List<OpenRouterMessage> Messages { get; set; } = new();

    [JsonPropertyName("response_format")]
    public OpenRouterResponseFormat ResponseFormat { get; set; } = new();
}

public class OpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OpenRouterResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_object";
}

public class OpenRouterResponse
{
    [JsonPropertyName("choices")]
    public List<OpenRouterChoice> Choices { get; set; } = new();
}

public class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterMessage Message { get; set; } = new();
}
