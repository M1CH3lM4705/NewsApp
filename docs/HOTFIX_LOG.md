# Log de Correções (Hotfix)

Este documento registra as correções críticas realizadas no fluxo de navegação e na interface de usuário do **NewsApp**.

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
