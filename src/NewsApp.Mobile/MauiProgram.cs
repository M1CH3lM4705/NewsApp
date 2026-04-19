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
		var assembly = Assembly.GetExecutingAssembly();
		string configName = "NewsApp.Mobile.appsettings.production.json";

#if DEBUG
		configName = "NewsApp.Mobile.appsettings.development.json";
#endif

		using var stream = assembly.GetManifestResourceStream(configName);
		if (stream != null)
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.Build();

			builder.Configuration.AddConfiguration(config);
			builder.Services.Configure<AppConfiguration>(config.GetSection("AppConfiguration"));
		}
	}
}
