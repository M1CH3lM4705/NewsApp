# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de solução e projetos para restaurar dependências
# Usaremos o NewsApp.Backend.slnf para restaurar apenas o necessário para o Web
COPY ["NewsApp.sln", "./"]
COPY ["NewsApp.Backend.slnf", "./"]
COPY ["src/NewsApp.Web/NewsApp.Web.csproj", "src/NewsApp.Web/"]
COPY ["src/NewsApp.Application/NewsApp.Application.csproj", "src/NewsApp.Application/"]
COPY ["src/NewsApp.Infrastructure/NewsApp.Infrastructure.csproj", "src/NewsApp.Infrastructure/"]
COPY ["src/NewsApp.Domain/NewsApp.Domain.csproj", "src/NewsApp.Domain/"]
COPY ["src/NewsApp.SharedUI/NewsApp.SharedUI.csproj", "src/NewsApp.SharedUI/"]
COPY ["tests/NewsApp.Tests/NewsApp.Tests.csproj", "tests/NewsApp.Tests/"]

RUN dotnet restore NewsApp.Backend.slnf

# Copiar o restante dos arquivos e buildar
COPY . .
WORKDIR "/src/src/NewsApp.Web"
RUN dotnet build "NewsApp.Web.csproj" -c Release -o /app/build

# Estágio de Publicação
FROM build AS publish
RUN dotnet publish "NewsApp.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NewsApp.Web.dll"]
