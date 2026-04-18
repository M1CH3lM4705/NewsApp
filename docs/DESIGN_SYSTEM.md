# Design System - NewsApp

O **NewsApp** utiliza um sistema de design moderno e limpo ("Modern Clean"), fundamentado nos princípios do Material Design através da biblioteca **MudBlazor**.

## 1. Paleta de Cores

Utilizamos uma paleta focada em legibilidade e contraste suave.

| Nome | Hex | Aplicação |
| :--- | :--- | :--- |
| **Primary** | `#594AE2` | Botões principais, ícones de destaque e branding. |
| **Secondary** | `#9C27B0` | Elementos de apoio e chips. |
| **Background (Light)** | `#F7F8FA` | Fundo principal da aplicação no modo claro. |
| **Background (Dark)** | `#1E1E2D` | Fundo principal da aplicação no modo escuro. |
| **Surface** | `#FFFFFF` / `#2D2D3B` | Cards, papéis e superfícies de elevação. |

## 2. Tipografia

A tipografia prioriza fontes sem serifa para uma aparência digital e limpa.

- **Família Principal**: Roboto, Inter.
- **Hierarquia**:
  - `h3/h4`: Para títulos de destaque (Hero e cabeçalhos).
  - `h6`: Para títulos de cards (em negrito).
  - `body1/body2`: Para textos corridos e resumos (itálico suave em resumos).

## 3. Ícones

Utilizamos a biblioteca **MudIcons** (baseada em Material Design Icons).
- **Sincronização**: `Icons.Material.Filled.Sync`
- **Dark Mode**: `Icons.Material.Filled.DarkMode` / `LightMode`
- **Check (Lidas)**: `Icons.Material.Filled.CheckCircle`

## 4. Componentes Customizados

### NewsCard
- **Visual**: Elevation="2", Outlined="true" (visual flat).
- **Interação**: Fade-out suave (opacity 0 -> 1) com transição de 400ms ao marcar como lida.

### Hero Section
- **Visual**: Gradiente de `Primary` para um violeta escuro (`#3a0647`).
- **Botão**: Estilo `rounded-pill` (pílula) para um toque amigável.

## 5. Estados Visuais

- **Loading**: Uso de `MudSkeleton` para evitar o "salto" de layout durante o carregamento de dados.
- **Empty States**: Uso de `MudAlert` ou `MudPaper` com mensagens claras quando não há conteúdo disponível.
