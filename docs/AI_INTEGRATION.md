# Integração com Gemini AI - NewsApp

Este documento detalha como o **NewsApp** utiliza a Inteligência Artificial (Google Gemini) para traduzir e resumir notícias em tempo real.

## 1. System Prompt

Para garantir que a IA se comporte como um tradutor jornalístico e retorne dados estruturados, utilizamos o seguinte **System Prompt**:

> "Você é um tradutor jornalístico especializado em notícias. Traduza o título para português e gere um resumo de no máximo 3 linhas em português. Retorne estritamente em formato JSON com as chaves 'translatedTitle' e 'shortSummary'."

## 2. Mapeamento de Dados (JSON to C#)

O Gemini é instruído via `generationConfig` (parâmetro `response_mime_type: "application/json"`) a retornar um JSON puro. Esse JSON é mapeado para a classe `TranslatedArticleDto`:

```csharp
public class TranslatedArticleDto
{
    [JsonPropertyName("translatedTitle")]
    public string TranslatedTitle { get; set; }

    [JsonPropertyName("shortSummary")]
    public string ShortSummary { get; set; }
}
```

A classe `GeminiTranslationService` realiza a limpeza de eventuais blocos de Markdown (```json) e desserializa o conteúdo para atualizar a entidade `NewsArticle`.

## 3. Estratégias de Performance e Latência

Como chamadas de IA podem ser lentas e caras, implementamos três níveis de otimização:

### A. Cache em Memória (IMemoryCache)
Antes de enviar qualquer notícia para o Gemini, o serviço verifica se o hash da URL da notícia já possui uma tradução armazenada em cache (TTL de 1 hora). Isso evita traduções repetidas da mesma notícia para diferentes usuários ou na mesma sessão.

### B. Processamento Paralelo (Task.WhenAll)
Em vez de traduzir uma notícia por vez de forma sequencial, utilizamos o `Task.WhenAll`. Isso dispara múltiplas requisições HTTP simultâneas para o Google AI Studio, reduzindo o tempo total de carregamento do feed de notícias.

### C. Fallback Resiliente
Se a cota da API (Free Tier) for atingida ou se houver qualquer erro de rede/IA, o sistema captura a exceção e retorna a notícia original em inglês. Isso garante que a aplicação nunca fique indisponível devido a falhas na camada de IA.

## 4. Limites do Tier Gratuito
- **Modelo**: `gemini-1.5-flash` (escolhido pela baixa latência).
- **Taxa de Requisição**: Atualmente limitado a 15 RPM (requisições por minuto) no tier gratuito. O uso de paralelismo deve ser monitorado para não exceder esse limite em ambientes de alta carga.
