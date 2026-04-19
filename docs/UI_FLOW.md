# Fluxo de UI - NewsApp

Este documento detalha o funcionamento da interface de usuário, a hierarquia de componentes e o gerenciamento de estado visual da aplicação.

## 1. Hierarquia de Componentes

A interface é composta por componentes reutilizáveis organizados da seguinte forma:

- **Home.razor (Página)**: Ponto de entrada que gerencia a sincronização inicial.
  - **NewsList.razor**: Componente de lista que filtra o conteúdo baseado no estado.
    - **NewsCard.razor**: Representação visual de uma notícia individual.
- **Archive.razor (Página)**: Visualização histórica de itens consumidos.

## 2. Máquina de Estados (Ciclo de Vida da Notícia)

As notícias transitam entre estados gerenciados pelo `INewsStateManager`:

1.  **Nova (Default)**: A notícia é retornada pela API e não consta no `HashSet` de lidas. É exibida no Feed da `Home.razor`.
2.  **Lendo (Ação)**: Ao clicar em "Acessar íntegra", o componente `NewsCard` invoca `MarkAsRead(id)`.
3.  **Lida (Estado Final)**: O ID da notícia é persistido na coleção `ReadArticles`. 
    - Na `Home.razor`, ela é filtrada e desaparece do feed.
    - Na `Archive.razor`, ela passa a ser listada na visão simplificada.

## 3. Persistência Local e Escopo

Atualmente, o estado é mantido utilizando **In-memory Scoped Storage**:

- **Escopo**: `Scoped`. O estado (`NewsStateManager`) é mantido enquanto a sessão do usuário (circuito Blazor) estiver ativa.
- **Comportamento**: Ao atualizar a página (F5), o estado é reiniciado. 
- **Evolução Futura**: Para persistência persistente (entre fechamentos de navegador), a implementação de `INewsStateManager` pode ser estendida para utilizar `localStorage` via JSInterop ou um banco de dados leve (SQLite).

## 4. UX e Design Patterns

- **CSS Isolation**: Cada componente possui seu próprio arquivo `.razor.css`, garantindo que estilos não vazem para outros elementos.
- **Skeleton/Loading States**: O botão de sincronização utiliza um estado visual de "Carregando" (`isSyncing`) para fornecer feedback imediato ao usuário durante chamadas de API.
- **Empty States**: Tanto o Feed quanto o Arquivo possuem tratamentos visuais para quando não há dados a exibir, evitando telas brancas.
