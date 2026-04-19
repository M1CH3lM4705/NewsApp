# Guia de Deploy e Execução Nativa Android - NewsApp

Este documento detalha como rodar o **NewsApp** em dispositivos físicos Android e como gerar o pacote final (APK).

## 1. Requisitos do Sistema
- **.NET 9 SDK**: Certifique-se de ter o SDK mais recente.
- **Carga de Trabalho MAUI**: Execute `dotnet workload install maui` no terminal.
- **Android SDK**: Geralmente instalado com o Visual Studio ou Android Studio.

## 2. Preparação do Dispositivo Físico (Sem Emulador)
Para economizar espaço em disco e memória RAM, recomendamos o uso de um smartphone físico via cabo USB.

### Passos no Celular:
1. Vá em **Configurações > Sobre o Telefone**.
2. Toque 7 vezes em **Número da Versão (Build Number)** para habilitar as "Opções do Desenvolvedor".
3. Volte e procure por **Sistema > Opções do Desenvolvedor**.
4. Ative a **Depuração USB (USB Debugging)**.
5. Conecte o celular ao computador via cabo USB de boa qualidade.

## 3. Execução Rápida via CLI
Com o celular conectado, você pode rodar o app diretamente sem abrir IDEs pesadas:

```powershell
# Lista os IDs dos dispositivos conectados
dotnet build -t:Run -f net9.0-android
```
*Este comando compila a versão debug, instala no celular e abre o app automaticamente.*

## 4. Publicação e Geração de APK (Release)
Para gerar o arquivo instalável final:

```powershell
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=apk -p:AndroidKeyStore=false
```
O arquivo APK será gerado em:
`src/NewsApp.Mobile/bin/Release/net9.0-android/publish/com.companyname.newsapp-Signed.apk`

### Assinatura do APK (Sideloading)
Para que o Android permita a instalação manual:
1. Gere uma keystore (apenas uma vez):
   `keytool -genkey -v -keystore my-release-key.keystore -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000`
2. No `.csproj` ou via argumentos do `dotnet publish`, aponte para este arquivo para assinar o app.

## 5. Teste de Layout e UI
- Como o app usa **Blazor Hybrid**, você pode redimensionar a janela do navegador no ambiente Desktop (F12 > Device Toolbar) para testar a responsividade antes de enviar para o celular.
- O **MudBlazor** ajustará o grid automaticamente de `lg="4"` (Desktop) para `xs="12"` (Mobile) conforme configurado nos componentes.
