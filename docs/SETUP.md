# Guia de Configuração (Setup)

Este documento orienta novos desenvolvedores na configuração do ambiente local para rodar o **NewsApp**.

## Pré-requisitos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.
- Uma IDE de sua preferência (Visual Studio 2022, JetBrains Rider ou VS Code).
- Uma chave de API válida da [NewsAPI](https://newsapi.org/) (opcionalmente do Gemini, se implementado).

---

## 1. Clonando o Repositório

```bash
git clone <URL_DO_REPOSITORIO>
cd NewsApp
```

## 2. Configurando Segredos (User Secrets)

Por questões de segurança, **não comitamos arquivos `appsettings.json` com chaves reais**. O projeto utiliza a ferramenta `dotnet user-secrets` para armazenar variáveis sensíveis localmente no seu sistema operacional.

Navegue até o diretório do projeto Web:
```bash
cd src/NewsApp.Web
```

*(Se for a primeira vez, inicialize os segredos no projeto - já deve estar inicializado se você baixou do repositório)*:
```bash
dotnet user-secrets init
```

Adicione suas chaves de API:
```bash
dotnet user-secrets set "AppConfiguration:NewsApiKey" "COLOQUE_SUA_CHAVE_DA_NEWS_API_AQUI"
dotnet user-secrets set "AppConfiguration:GeminiApiKey" "COLOQUE_SUA_CHAVE_DO_GEMINI_AQUI"
```

Para verificar se as chaves foram salvas corretamente:
```bash
dotnet user-secrets list
```

## 3. Restaurando e Construindo a Solução

Volte para a raiz da solução (`NewsApp/`) e execute a restauração e build de todos os projetos:

```bash
cd ../..
dotnet build NewsApp.sln
```

## 4. Rodando os Testes Automatizados

O projeto utiliza **xUnit** e **Moq** para testes unitários e de integração. Para garantir que tudo está funcionando corretamente antes de subir a aplicação, rode os testes:

```bash
dotnet test NewsApp.sln
```
*Você deve ver uma saída indicando que todos os testes passaram com sucesso.*

## 5. Executando a Aplicação Web

Para rodar a interface Blazor, execute o projeto Web:

```bash
dotnet run --project src/NewsApp.Web/NewsApp.Web.csproj
```

Abra o seu navegador e acesse a URL exibida no terminal (geralmente `http://localhost:5084` ou `https://localhost:7149`).
