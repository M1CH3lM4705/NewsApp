using NewsApp.Web.Components;
using NewsApp.Application.Configuration;
using NewsApp.Application.Interfaces;
using NewsApp.Application.Services;
using NewsApp.Application.UseCases;
using NewsApp.Infrastructure.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();

// 1. Configuração de CORS restritivo
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5084", "https://localhost:7149", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Configuração de Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiLimit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 30; // 30 requisições por minuto
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Mapeia a seção "AppConfiguration" do user-secrets/appsettings para a classe tipada
builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetSection("AppConfiguration"));

// Register NewsApp Services
builder.Services.AddHttpClient<INewsRepository, NewsApiService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "NewsApp-DotNet");
});

builder.Services.AddHttpClient<IGeminiTranslationService, GeminiTranslationService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

builder.Services.AddScoped<IGetLatestNewsUseCase, GetLatestNewsUseCase>();
builder.Services.AddScoped<INewsStateManager, NewsStateManager>();

var app = builder.Build();

// 3. Middlewares de Segurança
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    // CSP configurada para Blazor Server (requer unsafe-inline para o circuito SignalR)
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' _framework; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: https://generativelanguage.googleapis.com https://newsapi.org;");
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("SecurePolicy");
app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
