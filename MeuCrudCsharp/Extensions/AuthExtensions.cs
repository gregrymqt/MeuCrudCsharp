using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuCrudCsharp.Features.Auth;
using MeuCrudCsharp.Features.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MeuCrudCsharp.Extensions;

public static class AuthExtensions
{
    /// <summary>
    /// Configura os serviços de autenticação (JWT, Google) e autorização (políticas personalizadas).
    /// </summary>
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        // Limpa o mapeamento de claims padrão para evitar que o .NET renomeie as claims do JWT.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        
        // Define a cultura padrão para garantir consistência em formatação de datas e números.
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        // --- 1. Configuração da Autenticação ---
        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                var googleSettings = builder.Configuration.GetSection("Google").Get<GoogleSettings>();
                if (googleSettings?.ClientId is null || googleSettings?.ClientSecret is null)
                {
                    throw new InvalidOperationException("As credenciais do Google não foram encontradas.");
                }

                options.ClientId = googleSettings.ClientId;
                options.ClientSecret = googleSettings.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
                if (jwtSettings?.Key is null)
                {
                    throw new InvalidOperationException("A chave JWT não foi encontrada na configuração.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };
                
                // Evento para ler o token JWT de um cookie, caso não venha no header Authorization.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("jwt", out var tokenFromCookie))
                        {
                            context.Token = tokenFromCookie;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        // --- 2. Configuração da Autorização ---
        builder.Services.AddAuthorization(options =>
        {
            // Política básica que exige um usuário autenticado via JWT.
            options.AddPolicy("RequireJwtToken", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });

            // Política que exige um usuário autenticado e com uma assinatura ativa.
            options.AddPolicy("ActiveSubscription", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ActiveSubscriptionRequirement());
            });
        });

        return builder;
    }
}

