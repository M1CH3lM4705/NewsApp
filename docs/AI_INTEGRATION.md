# Integração com Gemini AI - NewsApp

Este documento detalha como o **NewsApp** utiliza a Inteligência Artificial (Google Gemini) para traduzir e resumir notícias em tempo real.

## 1. System Prompt

Para garantir que a IA se comporte como um tradutor jornalístico e retorne dados estruturados, utilizamos o seguinte **System Prompt**:

> "Você é um tradutor jornalístico. Traduza os títulos para português e gere resumos de no máximo 3 linhas em português. Receba uma lista de notícias e retorne um objeto JSON contendo um array chamado 'translations'. Cada item do array deve ter: 'url' (id único), 'title' e 'summary'."

## 2. Mapeamento de Dados (JSON to C#)

O Gemini é instruído via System Prompt a retornar um objeto JSON contendo as traduções. Esse JSON é mapeado para as classes `BulkTranslationResponse` e `GeminiTranslationItem`:

```csharp
public class BulkTranslationResponse
{
    [JsonPropertyName("translations")]
    public List<GeminiTranslationItem> Translations { get; set; } = new();
}

public class GeminiTranslationItem
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
```

A classe `GeminiTranslationService` realiza a limpeza de eventuais blocos de Markdown (```json) e desserializa o conteúdo. É feito um mapeamento cuidadoso cruzando a `Url` original com a `Url` devolvida pelo Gemini (via `FirstOrDefault`) para garantir resiliência caso a IA omita algum item.

## 3. Estratégias de Performance e Latência

Como chamadas de IA podem ser lentas e caras, implementamos três níveis de otimização:

### A. Cache em Memória (IMemoryCache)
Antes de enviar qualquer notícia para o Gemini, o serviço verifica se o hash da URL da notícia já possui uma tradução armazenada em cache (TTL de 1 hora). Isso evita traduções repetidas da mesma notícia para diferentes usuários ou na mesma sessão.

### B. Processamento em Lote (Bulk Translation)
Em vez de traduzir uma notícia por vez ou realizar múltiplas requisições paralelas (`Task.WhenAll`), enviamos a lista inteira de notícias não cacheadas em um único prompt para o Gemini. Isso otimiza drasticamente o uso da cota da API e reduz o overhead de rede.

### C. Fallback Resiliente
Se a cota da API (Free Tier) for atingida, se a resposta for inválida, ou se a IA omitir algum artigo específico do lote, o sistema mapeia graciosamente as falhas e retorna a notícia original em inglês. Isso garante que a aplicação nunca fique indisponível devido a falhas na camada de IA.

## 4. Limites do Tier Gratuito
- **Modelo**: `gemini-2.5-flash-lite` (escolhido pela baixa latência e melhor capacidade de seguir o formato JSON em lotes).
- **Taxa de Requisição**: Atualmente limitado a 15 RPM (requisições por minuto) no tier gratuito. O uso de paralelismo deve ser monitorado para não exceder esse limite em ambientes de alta carga.
