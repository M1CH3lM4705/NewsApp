# Segurança e Mitigação de Riscos

A segurança é uma prioridade no **NewsApp**. Este documento detalha as abordagens adotadas para mitigar vulnerabilidades comuns.

## 1. Proteção de Chaves de API (Secrets Management)
**Risco:** Exposição de chaves de API (`NewsApiKey`, `GeminiApiKey`) em repositórios públicos, permitindo o uso indevido e cobranças financeiras.

**Mitigação:**
- **.gitignore Robusto**: O arquivo `.gitignore` na raiz da solução bloqueia explicitamente o rastreamento de arquivos de configuração locais como `appsettings.json`, `appsettings.Development.json` e `secrets.json`.
- **User Secrets**: No ambiente de desenvolvimento, a aplicação utiliza a ferramenta `dotnet user-secrets`. As chaves são armazenadas em uma pasta protegida no perfil do usuário do Sistema Operacional (fora da árvore de diretórios do projeto), garantindo que nunca sejam comitadas acidentalmente.
- **Injeção Tipada (`IOptions`)**: As variáveis de ambiente são mapeadas para uma classe fortemente tipada (`AppConfiguration`). Isso evita a leitura direta de strings pelo código, reduzindo erros de digitação e facilitando a validação das configurações no momento do *startup*.

## 2. Tratamento de Exceções e Falhas de Rede
**Risco:** Falhas de comunicação com APIs externas podem gerar exceções não tratadas (`Unhandled Exceptions`), derrubando a aplicação (Denial of Service local) ou expondo *Stack Traces* detalhados para o usuário final, o que pode revelar a estrutura interna da aplicação (Information Disclosure).

**Mitigação:**
- O serviço `NewsApiService` encapsula todas as chamadas HTTP dentro de um bloco `try-catch` específico.
- **Captura Específica**: Capturamos `HttpRequestException` para problemas de rede (timeout, DNS) e validamos o `IsSuccessStatusCode` da resposta HTTP.
- **Falha Segura (Fail-Safe)**: Em caso de erro, o serviço loga o detalhe da falha internamente (usando `ILogger`) e retorna uma coleção vazia (`Enumerable.Empty<NewsArticle>()`) para a camada superior. Isso garante que a UI continue funcionando graciosamente, mesmo que a fonte de dados externa esteja indisponível.

## 3. Segurança no Front-End (Blazor)
**Risco:** Ataques de Cross-Site Scripting (XSS) e Cross-Site Request Forgery (CSRF).

**Mitigação:**
- **Razor Engine**: O framework Blazor Web App codifica automaticamente (HTML encoding) todas as strings renderizadas no DOM (usando o símbolo `@`). Isso previne a injeção de scripts maliciosos (XSS) vindos de APIs de notícias de terceiros.
- **Antiforgery**: O middleware `UseAntiforgery()` está configurado no `Program.cs` para mitigar ataques CSRF na submissão de eventuais formulários dentro da aplicação.

## Próximos Passos (Produção)
Para um ambiente produtivo, recomenda-se:
- Substituir o User Secrets por um provedor de segredos robusto, como o **Azure Key Vault** ou **AWS Key Management Service (KMS)**.
- Implementar políticas de **Retry e Circuit Breaker** (ex: usando a biblioteca *Polly*) nas chamadas HttpClient para aumentar a resiliência contra instabilidades temporárias das APIs externas.
