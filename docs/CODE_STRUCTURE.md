# Estrutura de Código (Code-Behind Pattern)

Este documento detalha a adoção do padrão **Code-Behind** para o desenvolvimento de componentes e páginas Blazor no projeto **NewsApp**.

## 1. Por que adotar o Code-Behind?

Originalmente, o Blazor incentiva o uso do bloco `@code { ... }` dentro do arquivo `.razor`. No entanto, à medida que a aplicação cresce, essa abordagem mistura marcação UI com lógica de negócio complexa, dificultando a manutenção.

Adotamos o padrão Code-Behind utilizando **Partial Classes** (`.razor.cs`) pelos seguintes motivos:

- **Separação de Responsabilidades**: A marcação (HTML/MudBlazor) fica isolada no arquivo `.razor`, enquanto a lógica C# reside no arquivo `.cs`.
- **Organização**: Facilita o uso de ferramentas de análise de código estático e refatoração da IDE.
- **Testabilidade**: Torna as classes parciais acessíveis para frameworks de testes unitários como **bUnit**, facilitando o mock de dependências e a validação de estados internos.

## 2. Injeção de Dependências

Em arquivos `.razor`, utilizamos a diretiva `@inject`. Nas classes de Code-Behind, convertemos essas injeções para propriedades decoradas com o atributo `[Inject]`:

```csharp
public partial class NewsList
{
    [Inject] 
    private INewsStateManager StateManager { get; set; } = null!;

    // ... restante da lógica
}
```

## 3. Impacto nos Testes Unitários de UI (bUnit)

O uso de Code-Behind permite que os testes unitários de interface:

1.  **Instanciem o Componente de forma isolada**: É possível injetar mocks diretamente no contêiner de serviços do bUnit.
2.  **Acessem Propriedades e Métodos**: Através da classe parcial, o teste pode verificar se uma propriedade de estado (`isLoading`, por exemplo) foi alterada corretamente após uma ação, sem depender exclusivamente da inspeção do DOM renderizado.

## 4. Fluxo de Renderização Thread-Safe

Ao manipular coleções ou estados que disparam re-renderizações (especialmente após `Task.Delay` ou chamadas assíncronas de API), utilizamos o padrão:

```csharp
await InvokeAsync(StateHasChanged);
```

Isso garante que a sinalização de mudança de estado ocorra no contexto da thread de UI do Blazor, evitando exceções de renderização concorrente e garantindo que o **MudBlazor** re-organize o layout (como o grid de notícias) de forma imediata e correta.
