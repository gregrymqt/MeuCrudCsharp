using MeuCrudCsharp.Features.MercadoPago.Base;
using Microsoft.AspNetCore.HttpOverrides;

namespace MeuCrudCsharp.Extensions;

public static class WebServicesExtensions
{
    // Define o nome da política de CORS como uma constante para evitar "magic strings".
    public const string CorsPolicyName = "_myAllowSpecificOrigins";
    
    /// <summary>
    /// Configura os serviços web essenciais, incluindo HttpClient para APIs externas,
    /// políticas de CORS, tratamento de cookies e suporte para proxy reverso (Forwarded Headers).
    /// </summary>
    public static WebApplicationBuilder AddWebServices(this WebApplicationBuilder builder)
    {
        // --- 1. Configuração do HttpClient para a API do Mercado Pago ---
        builder.Services.AddHttpClient("MercadoPagoClient", client =>
        {
            var mercadoPagoSettings = builder.Configuration.GetSection("MercadoPago").Get<MercadoPagoSettings>();
            if (mercadoPagoSettings is null || string.IsNullOrEmpty(mercadoPagoSettings.AccessToken))
            {
                throw new InvalidOperationException(
                    "Configurações do Mercado Pago não encontradas ou o AccessToken está vazio.");
            }

            client.BaseAddress = new Uri("https://api.mercadopago.com");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mercadoPagoSettings.AccessToken);
        });

        // --- 2. Configuração da Política de CORS ---
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: CorsPolicyName,
                policy =>
                {
                    policy.WithOrigins(
                            "https://214aeb274764.ngrok-free.app", // Exemplo para Ngrok
                            "http://localhost:5045"             // Exemplo para desenvolvimento local
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        // --- 3. Configuração para Confiança em Headers de Proxy (Essencial para Ngrok/IIS/Nginx) ---
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        // --- 4. Configuração de Cookies ---
        builder.ConfigureCookiePolicies();

        return builder;
    }

    /// <summary>
    /// Configura o comportamento dos cookies da aplicação, incluindo o tratamento
    /// de redirecionamentos para APIs e o SameSite para logins externos.
    /// </summary>
    private static void ConfigureCookiePolicies(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/ExternalLogin";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            
            // Lógica para APIs: em vez de redirecionar, retorna códigos de erro HTTP
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        // Ajusta a política de SameSite para ser compatível com logins externos (ex: Google)
        builder.Services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
            options.OnAppendCookie = cookieContext =>
            {
                // Aplica SameSite=None apenas para cookies específicos do processo de login externo
                if (cookieContext.CookieName.StartsWith(".AspNetCore.Correlation.") || 
                    cookieContext.CookieName.StartsWith(".AspNetCore.OpenIdConnect.Nonce."))
                {
                    cookieContext.CookieOptions.SameSite = SameSiteMode.None;
                }
            };
        });
    }
}
