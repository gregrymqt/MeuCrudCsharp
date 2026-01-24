using System.Text.Json;

namespace MeuCrudCsharp.Extensions;

public static class WebAppBuilderExtensions
{
    /// <summary>
    /// Configura os serviços essenciais do ASP.NET Core, como Controllers, Razor Pages,
    /// SignalR, e ferramentas de desenvolvimento como o Swagger.
    /// </summary>
    public static WebApplicationBuilder AddCoreServices(this WebApplicationBuilder builder)
    {
        // 1. Adiciona suporte para Controllers com Views e configura a serialização JSON.
        builder
            .Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        // 2. Adiciona suporte para Razor Pages e define as regras de autorização.
        builder.Services.AddRazorPages(options =>
        {
            // Regra Geral: Proteger todas as páginas por padrão, exigindo um JWT.
            options.Conventions.AuthorizeFolder("/", "RequireJwtToken");

            // Exceções: Permitir acesso anônimo a páginas públicas específicas.
            options.Conventions.AllowAnonymousToPage("/Index");
            options.Conventions.AllowAnonymousToPage("/Account/ExternalLogin");
            options.Conventions.AllowAnonymousToPage("/Account/Logout");
        });

        // 3. Adiciona os serviços fundamentais para uma API web.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 4. Adiciona o serviço do SignalR para comunicação em tempo real.
        builder.Services.AddSignalR();

        return builder;
    }
}
