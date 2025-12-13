using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuCrudCsharp.Features.Auth.Middlewares; // Importante: Referência ao Middleware criado
using MeuCrudCsharp.Features.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MeuCrudCsharp.Extensions;

public static class AuthExtensions
{
    /// <summary>
    /// Configura os SERVIÇOS de autenticação (JWT, Google) e autorização (DI).
    /// </summary>
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        // Limpa o mapeamento padrão de claims [cite: 4]
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // Configuração de Cultura [cite: 5]
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        // --- 1. Configuração da Autenticação ---
        builder.Services.AddAuthentication(options => 
        {
            // Definimos os schemes padrões explicitamente para evitar ambiguidades
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddGoogle(options =>
        {
            var googleSettings = builder.Configuration.GetSection("Google").Get<GoogleSettings>();
            
            if (googleSettings?.ClientId is null || googleSettings?.ClientSecret is null)
                throw new InvalidOperationException("Credenciais do Google não encontradas."); // [cite: 7]

            options.ClientId = googleSettings.ClientId;
            options.ClientSecret = googleSettings.ClientSecret;
            options.SignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
            
            if (jwtSettings?.Key is null)
                throw new InvalidOperationException("Chave JWT não encontrada."); // [cite: 9]

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)), // [cite: 11]
                ValidateIssuer = false,
                ValidateAudience = false,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role, // [cite: 12]
                ClockSkew = TimeSpan.Zero // Importante: Remove o tempo de tolerância padrão de 5min
            };

           // Evento para ler Token de Cookies (se necessário) [cite: 14]
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("jwt", out var tokenFromCookie))
                    {
                        context.Token = tokenFromCookie;
                    }
                    return Task.CompletedTask;
                },
                // Opcional: Logar falhas de autenticação para debug
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Token inválido: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
        });

        // --- 2. Configuração da Autorização ---
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireJwtToken", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });

            options.AddPolicy("ActiveSubscription", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ActiveSubscriptionRequirement()); // [cite: 21]
            });
        });

        return builder;
    }

    /// <summary>
    /// NOVO MÉTODO: Configura o PIPELINE (Ordem de execução).
    /// Deve ser chamado no Program.cs no lugar de app.UseAuthentication() e app.UseAuthorization()
    /// </summary>
    public static WebApplication UseAuthFeatures(this WebApplication app)
    {
        // 1. O Guardião (Middleware de Blacklist) roda PRIMEIRO
        // Ele verifica no Redis se o token foi revogado antes mesmo do .NET tentar validar a assinatura
        app.UseMiddleware<JwtBlacklistMiddleware>();

        // 2. Se o token não estiver na blacklist, o .NET valida a assinatura e cria o User Principal
        app.UseAuthentication();

        // 3. Por fim, verifica se o User Principal tem permissão de acesso
        app.UseAuthorization();

        return app;
    }
}