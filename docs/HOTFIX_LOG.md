# Log de Correções (Hotfix)

Este documento registra as correções críticas realizadas no fluxo de navegação e na interface de usuário do **NewsApp**.

## 5. Desacoplamento do Projeto Mobile no CI (NETSDK1147)

### Problema
O build do GitHub Actions falhava com o erro `NETSDK1147` ao tentar restaurar a solução completa (`NewsApp.sln`). Isso ocorria porque o projeto `NewsApp.Mobile` (MAUI) exige cargas de trabalho (workloads) específicas do Android que não estão pré-instaladas nos runners padrão do GitHub, tornando a esteira de CI lenta e propensa a falhas de ambiente.

### Causa
O comando `dotnet restore` em uma solução tenta resolver dependências de todos os projetos nela contidos. Projetos MAUI com `net9.0-android` possuem dependências de SDKs nativos que não são necessários para validar a lógica de domínio, aplicação ou a interface web.

### Solução
- **Solution Filter (SLNF)**: Criamos o arquivo `NewsApp.Backend.slnf` que inclui apenas os projetos de core e testes, excluindo explicitamente o projeto mobile. Os workflows de CI (`tests.yml` e `lint.yml`) foram atualizados para executar comandos contra este filtro em vez da solução completa. Isso garante que o `dotnet restore` e `dotnet build` nunca tentem processar o projeto Mobile no ambiente de CI.
- **Estabilização do SDK**: Criamos um arquivo `global.json` na raiz do projeto fixando o SDK na versão `9.0.100` com `rollForward: latestFeature` para garantir consistência entre o desenvolvimento local e o ambiente de CI.
- **Resultado**: A esteira de CI agora valida apenas o core do sistema (Web, Application, Domain, Infrastructure e Tests), mantendo-se rápida, confiável e totalmente imune ao erro `NETSDK1147`.

## 1. Correção de Espaçamento no Grid de Notícias

### Problema
Ao marcar uma notícia como lida, o card desaparecia visualmente (opacity 0), mas o elemento DOM permanecia na árvore, deixando um "espaço em branco" (vácuo) no layout, especialmente visível no `MudStack`.

### Causa (Ciclo de Vida do Blazor)
O Blazor re-renderiza componentes baseados em alterações de estado. No entanto, se a coleção pai não for notificada para remover o item da iteração (`foreach`), o componente filho continua existindo. 

### Solução
- Substituímos o `MudStack` por `MudGrid` + `MudItem` no `NewsList.razor`.
- Implementamos uma propriedade calculada `.ToList()` para garantir que a coleção filtrada seja imutável durante o ciclo de renderização.
- Adicionamos um `Task.Delay(450)` no método de atualização para permitir que a animação de CSS `fade-out` termine antes do Blazor remover o nó do DOM, garantindo que os itens restantes se re-organizem suavemente para preencher o espaço.

## 2. Navegação Externa Segura

### Problema
O botão "Ler Mais" não abria a notícia original de forma eficiente e não disparava a limpeza do feed simultaneamente.

### Solução
- Injetamos o `IJSRuntime` no `NewsCard.razor`.
- Utilizamos o comando `window.open(url, '_blank')` via JS Interop. Esta abordagem é mais segura e flexível em aplicações SPA (Single Page Applications) do que o uso de tags `<a>` diretas quando há lógica de backend associada ao clique (como marcar como lida).
- O fluxo agora é: **Marcar no Backend -> Disparar Animação -> Abrir Nova Aba -> Remover do Feed**.

## 3. Gestão de Histórico (Rota /lidas)

### Problema
Não havia uma forma do usuário visualizar o que já foi lido ou recuperar uma notícia removida acidentalmente.

### Solução
- Criamos a página `Archive.razor` com a rota `/lidas`.
- Adicionamos o método `MarkAsUnread` no `INewsStateManager`.
- Implementamos uma tabela de histórico que permite a **Restauração** da notícia, removendo-a do histórico e fazendo-a reaparecer no feed principal instantaneamente através do estado compartilhado (`Scoped`).

## 4. Correção de Chaves Duplicadas (Blazor) e Fallback do Gemini

### Problema
A aplicação apresentava o erro "System.InvalidOperationException: More than one sibling of component 'MudBlazor.MudItem' has the same key value" resultando em uma tela de erro 500. Isso ocorria simultaneamente a um erro no `GeminiTranslationService` ("Sequence contains no matching element").

### Causa
A NewsAPI ocasionalmente retorna a mesma notícia (mesma URL) mais de uma vez na mesma resposta. Como o `Id` das entidades era gerado deterministicamente a partir do hash da URL, artigos duplicados recebiam o mesmo `Id`, violando a regra de chaves únicas (`@key`) do laço `@foreach` no componente `NewsList.razor`. Além disso, a tradução em lote (`Bulk`) do Gemini assumia que todos os itens seriam sempre traduzidos e retornados (`.First()`); se o Gemini omitisse ou alterasse sutilmente a URL de um artigo no JSON de resposta, ocorria uma exceção que invalidava todo o lote.

### Solução
- **Deduplicação na Infraestrutura**: Atualizamos o `NewsApiService` para filtrar os resultados brutos aplicando um `GroupBy(a => a.Url).Select(g => g.First())` antes de gerar os GUIDs.
- **Proteção no Estado da UI**: Modificamos o método `SyncNews` no `Home.razor.cs` para adicionar artigos ao estado `_articles` apenas se suas URLs ainda não existirem na lista (substituindo o antigo `AddRange`).
- **Resiliência na Tradução**: No `GeminiTranslationService`, alteramos o mapeamento do resultado bulk de `.First()` para `.FirstOrDefault()`, ignorando itens omitidos e permitindo que o sistema caia suavemente no comportamento de Fallback (retornando as notícias originais não traduzidas) em vez de causar um crash completo.
