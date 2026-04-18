# Fluxo de Obtenção de Dados (Data Fetching) - NewsApp

Este documento detalha como a aplicação gerencia a busca, paginação, filtragem e cache das notícias consumidas da NewsAPI.

## 1. Paginação (Infinite Scroll / Load More)

A aplicação utiliza uma estratégia de **Server-side Pagination** baseada em números de página:

- **Parâmetros**: `page` (número da página) e `pageSize` (quantidade de itens por página, fixo em 10).
- **Fluxo**: 
  1. No carregamento inicial ou troca de categoria, a página é resetada para `1`.
  2. Ao clicar em "Carregar Mais", o estado interno do componente incrementa a página (`_currentPage++`).
  3. A nova lista de artigos retornada pela API é anexada (`AddRange`) à lista existente na UI, criando o efeito de rolagem contínua.

## 2. Filtros e Busca por Palavra-Chave

Os filtros operam de forma integrada para refinar os resultados:

- **Categorias**: Utiliza o componente `MudChipSet`. A seleção de uma categoria (ex: Tecnologia) dispara uma nova busca limpando o feed atual.
- **Busca (Search)**: Um campo de texto permite filtrar por termos específicos. A busca utiliza o parâmetro `q` da NewsAPI.
- **Interseção**: É possível filtrar por categoria e palavra-chave simultaneamente (ex: notícias de "Saúde" que contenham "vacina").

## 3. Otimização e Cache em Memória

Para evitar o consumo excessivo de créditos da API e melhorar a responsividade:

- **Cache na Application**: Implementamos um dicionário estático no `GetLatestNewsUseCase` que armazena os resultados por 5 minutos.
- **Chave de Cache**: A chave é composta pela combinação de `categoria-query-pagina-pageSize`.
- **Benefício**: Se o usuário alternar entre as abas "Tecnologia" e "Negócios" rapidamente, os dados serão recuperados instantaneamente do cache sem realizar uma nova chamada de rede.

## 4. Limites e Rate Limiting (NewsAPI)

O projeto utiliza o tier **Developer** da NewsAPI, que possui restrições importantes:

- **Requisições**: Limite de 100 requisições por dia.
- **Artigos Antigos**: Apenas notícias de até 1 mês atrás podem ser buscadas.
- **Atraso**: As notícias podem ter um atraso de até 15-30 minutos em relação ao tempo real.
- **Mitigação**: O cache implementado na camada Application ajuda a preservar o limite de requisições diárias durante a navegação do usuário.
