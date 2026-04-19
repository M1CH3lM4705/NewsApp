# Gerenciamento de Variáveis e Segredos Mobile (.NET MAUI)

Este documento descreve como o **NewsApp** lida com segredos de forma segura no Android, evitando exposição no repositório.

## 1. Embedded Resources (`appsettings.json`)
Diferente de apps web que buscam arquivos no disco, apps mobile embutem as configurações diretamente no binário (DLL).

- **Desenvolvimento**: Edite `src/NewsApp.Mobile/appsettings.development.json` (apenas para chaves de teste).
- **Produção**: O arquivo `appsettings.production.json` é populado automaticamente durante o build de CI/CD.

### Onde os arquivos ficam no APK?
Após a compilação, o JSON é compilado como um fluxo de bytes dentro da DLL principal do projeto. Ele não é visível como um arquivo solto na pasta de assets do APK, o que dificulta a extração simples por ferramentas de inspeção.

## 2. Injeção de Segredos via MSBuild (Segurança no CI/CD)
Para evitar salvar chaves reais no Git, o `.csproj` contém um target chamado `InjectSecrets`.

Ao gerar o APK de Release, use:
```powershell
dotnet publish -f net9.0-android -c Release -p:NewsApiKey="SUA_CHAVE_AQUI" -p:GeminiApiKey="SUA_CHAVE_IA"
```
Isso criará o JSON de produção em tempo de memória no build agent, garantindo que o segredo nunca toque o seu repositório de código.

## 3. SecureStorage (Criptografia Nativa)
Para chaves que o usuário pode configurar dentro do app, utilizamos o `SecureStorageService`.

- **Android**: Utiliza o **Android Keystore** e as APIs de criptografia do sistema operacional para salvar os dados em repouso.
- **Implementação**: A classe `SecureStorageService` implementa o `ISecureStorageService` injetado via DI.

```csharp
// Exemplo de uso:
await _secureStorage.SaveSecretAsync("UserGeminiKey", "AI_KEY_FROM_UI");
```

## 4. Avisos Críticos de Segurança
1. **NUNCA** faça commit de chaves reais nos arquivos `appsettings.json`.
2. Adicione `appsettings.development.json` ao `.gitignore` se ele contiver chaves privadas de uso pessoal.
3. Utilize o comando `dotnet user-secrets` apenas para desenvolvimento local; ele não funciona dentro do Android nativo.
