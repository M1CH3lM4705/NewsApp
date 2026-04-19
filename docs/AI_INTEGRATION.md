# Integração com IA e Sistema de Failover - NewsApp

Este documento detalha como o **NewsApp** utiliza Inteligência Artificial (Google Gemini e OpenRouter) para traduzir e resumir notícias em tempo real, garantindo alta disponibilidade através de um fluxo de failover automático.

## 1. Arquitetura de Tradução

O sistema utiliza o padrão **Proxy/Orchestrator** para gerenciar múltiplos provedores de IA. A lógica comum de cache, processamento de prompt e desserialização é centralizada em uma classe base abstrata.

### Fluxo de Decisão (TranslationOrchestrator)
1.  **Tentativa Primária (Gemini)**: O sistema tenta realizar a tradução utilizando o `GeminiTranslationService`.
2.  **Detecção de Quota (Erro 429)**: Caso o Gemini retorne um erro de excesso de requisições, o orquestrador captura a exceção especificamente.
3.  **Failover Automático (OpenRouter)**: O sistema aciona o `OpenRouterTranslationService` utilizando o modelo `qwen/qwen3-coder:free` como backup imediato.
4.  **Fallback Final**: Se ambos os serviços falharem ou estiverem indisponíveis, o sistema retorna os artigos originais (em inglês), garantindo que o usuário nunca seja interrompido por erros técnicos de IA.

## 2. System Prompt Unificado

Para manter a consistência entre diferentes modelos de linguagem, utilizamos um prompt centralizado na `BaseTranslationService`:

> "Você é um tradutor jornalístico. Traduza os títulos para português e gere resumos de no máximo 3 linhas em português. Receba uma lista de notícias e retorne um objeto JSON contendo um array chamado 'translations'. Cada item do array deve ter: 'url' (id único), 'title' e 'summary'."

## 3. Provedores e Modelos

| Provedor | Modelo | Papel | Notas |
| :--- | :--- | :--- | :--- |
| **Google Gemini** | `gemini-2.5-flash-lite` | Primário | Alta performance e precisão no formato JSON. |
| **OpenRouter** | `qwen/qwen3-coder:free` | Backup | Modelo robusto para instruções de formato (JSON) e código. |

## 4. Estratégias de Performance e Resiliência

### A. Cache em Memória (IMemoryCache)
Implementado na classe base, o cache verifica o hash da URL da notícia antes de qualquer chamada externa. O tempo de vida (TTL) é de 1 hora, reduzindo drasticamente os custos e a latência.

### B. Processamento em Lote (Bulk Translation)
Ambos os serviços realizam traduções em lote. O sistema projeta apenas os campos necessários (`Url`, `Title`, `Description`) para o prompt, minimizando o consumo de tokens.

### C. Segurança e Headers
- **OpenRouter**: Configurado com headers `HTTP-Referer` e `X-Title` para conformidade com a política da plataforma.
- **Failover Transparente**: O usuário não percebe a troca de provedor, apenas a manutenção da funcionalidade.

## 5. Limites e Configuração
As chaves de API (`GeminiApiKey` e `OpenRouterApiKey`) são gerenciadas via `AppConfiguration` e devem ser configuradas via Variáveis de Ambiente ou User Secrets para evitar exposição.
