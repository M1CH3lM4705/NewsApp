using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Application.Services;
using NewsApp.Application.UseCases;
using NewsApp.Infrastructure.Services;
using NewsApp.Mobile.Services;

namespace NewsApp.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// 1. TRANSICÃO DE VARIÁVEIS DE AMBIENTE (Segurança Mobile)
		LoadEmbeddedConfiguration(builder);

		// 2. SEGURANÇA ADICIONAL (SecureStorage Nativo)
		builder.Services.AddScoped<ISecureStorageService, SecureStorageService>();

		// Registro de Serviços Core
		builder.Services.AddMemoryCache();
		builder.Services.AddScoped<INewsStateManager, NewsStateManager>();
		builder.Services.AddTransient<INewsRepository, NewsApiService>();
		builder.Services.AddTransient<IGeminiTranslationService, GeminiTranslationService>();
		builder.Services.AddScoped<IGetLatestNewsUseCase, GetLatestNewsUseCase>();

		builder.Services.AddScoped(sp => new HttpClient());
		builder.Services.AddMudServices();

		return builder.Build();
	}

	private static void LoadEmbeddedConfiguration(MauiAppBuilder builder)
	{
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			var configBuilder = new ConfigurationBuilder();

			// Tenta carregar produção
			Console.WriteLine("DEBUG: Carregando configuração de produção...");
			using (var streamProd = assembly.GetManifestResourceStream("NewsApp.Mobile.appsettings.production.json"))
			{
				if (streamProd != null && streamProd.Length > 0) 
                    configBuilder.AddJsonStream(streamProd);
			}

			// Tenta carregar development (sobrescreve produção se em DEBUG)
#if DEBUG
			using (var streamDev = assembly.GetManifestResourceStream("NewsApp.Mobile.appsettings.development.json"))
			{
				if (streamDev != null && streamDev.Length > 0) 
                    configBuilder.AddJsonStream(streamDev);
			}
#endif

			// Adiciona suporte a variáveis de ambiente (para CI/CD e Segredos do GitHub)
			configBuilder.AddEnvironmentVariables();

			var config = configBuilder.Build();
			builder.Configuration.AddConfiguration(config);
			builder.Services.Configure<AppConfiguration>(config.GetSection("AppConfiguration"));
		}
		catch (Exception ex)
		{
			// Silenciosamente falha para evitar crash no startup
			Console.WriteLine($"CRITICAL_ERROR: Falha ao carregar configurações: {ex.Message}");
		}
	}
}
