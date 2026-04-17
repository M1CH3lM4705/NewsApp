# Arquitetura do NewsApp

O **NewsApp** foi desenhado seguindo os princípios da **Clean Architecture** (Arquitetura Limpa), visando a separação de responsabilidades, testabilidade e independência de frameworks externos.

## Estrutura de Camadas

A solução está dividida em 4 camadas principais:

### 1. Domain (`NewsApp.Domain`)
- **Responsabilidade**: Contém as regras de negócio centrais e invariáveis da aplicação.
- **Componentes**: Entidades (ex: `NewsArticle`), Objetos de Valor e Enums.
- **Dependências**: Não depende de nenhuma outra camada. É o núcleo do sistema.

### 2. Application (`NewsApp.Application`)
- **Responsabilidade**: Orquestra os fluxos de negócio (Casos de Uso) da aplicação.
- **Componentes**: 
  - Interfaces de Casos de Uso (ex: `IGetLatestNewsUseCase`) e suas implementações.
  - Interfaces de Repositório (ex: `INewsRepository`) para inversão de dependência.
  - DTOs e Classes de Configuração (ex: `AppConfiguration`).
- **Dependências**: Depende estritamente da camada `Domain`.

### 3. Infrastructure (`NewsApp.Infrastructure`)
- **Responsabilidade**: Implementa as interfaces definidas na `Application`, lidando com detalhes técnicos como acesso a banco de dados, APIs externas e file system.
- **Componentes**:
  - Implementação de Repositórios (ex: `NewsApiService`).
  - DTOs para consumo de APIs externas (ex: `NewsApiResponse`, `NewsApiArticle`).
- **Dependências**: Depende da camada `Application` (para implementar suas interfaces).

### 4. Web (`NewsApp.Web`)
- **Responsabilidade**: Interface de usuário e ponto de entrada da aplicação.
- **Componentes**:
  - Componentes Blazor (UI).
  - Configuração de Injeção de Dependência (`Program.cs`).
  - Gerenciamento de middlewares.
- **Dependências**: Depende das camadas `Application` e `Infrastructure`.

---

## Fluxo de Dados (Data Flow)

O fluxo de dados para a funcionalidade de "Buscar Últimas Notícias" ocorre da seguinte maneira:

1. **Ação do Usuário (Web)**: O usuário acessa a página inicial no Blazor (`NewsApp.Web`). O componente Blazor injeta a interface `IGetLatestNewsUseCase`.
2. **Caso de Uso (Application)**: A camada Web chama o método `ExecuteAsync()` do caso de uso. O caso de uso, por sua vez, depende da interface `INewsRepository` para buscar os dados brutos.
3. **Acesso Externo (Infrastructure)**: A implementação `NewsApiService` (que resolve `INewsRepository`) é invocada.
   - Ela obtém a `NewsApiKey` segura via `IOptions<AppConfiguration>`.
   - Realiza uma requisição HTTP via `HttpClient` para a NewsAPI.
   - Desserializa a resposta JSON para os DTOs internos (`NewsApiResponse`).
4. **Mapeamento para o Domínio**: Ainda na Infrastructure, os DTOs da API são convertidos para a entidade de domínio `NewsArticle` e retornados para a Application.
5. **Regra de Negócio (Application)**: O caso de uso recebe as entidades `NewsArticle` e aplica a regra de negócio (ex: filtra para retornar apenas as notícias publicadas *hoje*).
6. **Retorno (Web)**: A lista final filtrada de `NewsArticle` é devolvida ao componente Blazor, que a renderiza na tela para o usuário.

Esta separação garante que, caso a API de notícias mude amanhã, apenas a camada de `Infrastructure` precisará ser modificada, mantendo o restante do sistema intacto.
