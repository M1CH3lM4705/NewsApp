# Segurança e Proteção de Dados (Security Advisory)

Este documento detalha as medidas de segurança, privacidade e proteção contra ameaças implementadas no **NewsApp**.

## 1. Modelo de Confiança e Proxy Backend

Para garantir a proteção total de credenciais e chaves de API, o **NewsApp** utiliza um modelo de **Proxy no Servidor**:

- **Navegador (Client-side)**: A interface Blazor (UI) nunca tem acesso às chaves de API (`NewsApiKey` ou `GeminiApiKey`).
- **Servidor (Blazor Server)**: Todo o processamento lógico, comunicação com APIs de terceiros e gerenciamento de segredos ocorre exclusivamente no servidor. 
- **Isolamento**: As chaves são injetadas via Variáveis de Ambiente ou `dotnet user-secrets` e são consumidas apenas por serviços rodando no backend.

## 2. Proteção contra Injeções

Implementamos múltiplas camadas de defesa contra ataques de injeção:

- **XSS e Sanitização**: Todos os inputs de usuário (como o campo de busca e categorias) são sanitizados utilizando `WebUtility.HtmlEncode`. O comprimento das queries é limitado para evitar ataques de estouro ou negação de serviço.
- **Prompt Injection (IA)**: No serviço de tradução do Gemini, utilizamos técnicas de **Defensive Prompting**. O conteúdo das notícias é delimitado por marcadores estritos (`<<< CONTEÚDO >>>`) e a IA recebe instruções explícitas para ignorar comandos ou perguntas embutidas no texto original, atuando apenas como tradutora.

## 3. Segurança na Comunicação e Headers

A aplicação configura headers de segurança recomendados pela OWASP no middleware do ASP.NET:

- **X-Content-Type-Options: nosniff**: Evita que o navegador tente "adivinhar" o tipo de conteúdo, prevenindo ataques de MIME-sniffing.
- **X-Frame-Options: DENY**: Impede que a aplicação seja carregada dentro de um iframe, protegendo contra ataques de *Clickjacking*.
- **Content-Security-Policy (CSP)**: Implementamos uma política restritiva que permite apenas o carregamento de recursos de fontes confiáveis (própria origem, Google Fonts e NewsAPI), mitigando XSS.
- **HSTS (Strict-Transport-Security)**: Garante que toda a comunicação seja feita via HTTPS.

## 4. Rate Limiting (Prevenção de DoS)

Para evitar ataques de força bruta, raspagem de dados em massa (scraping) ou negação de serviço que possam esgotar os créditos das APIs, implementamos o **Microsoft.AspNetCore.RateLimiting**:

- **Política**: Janela fixa de 1 minuto.
- **Limite**: Máximo de 30 requisições por minuto por IP/Sessão.
- **Resposta**: Retorno automático de `429 Too Many Requests` ao exceder o limite.

## 5. Reportando Vulnerabilidades

Se você encontrar alguma falha de segurança no NewsApp, por favor:

1. **NÃO abra uma Issue pública** no GitHub.
2. Envie um e-mail detalhado para a equipe de desenvolvimento (miche... [seu e-mail]).
3. Forneça o passo a passo para reproduzir a vulnerabilidade.

Nós nos comprometemos a analisar e corrigir falhas críticas em até 48 horas.
