using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions; // Importando nossas exceções customizadas
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http; // Necessário para HttpContext
using Microsoft.Extensions.Logging; // MUDANÇA 1: Adicionando a dependência do Logger

namespace MeuCrudCsharp.Features.Auth
{
    public class AppAuthService : IAppAuthService
    {
        private readonly ILogger<AppAuthService> _logger; // MUDANÇA 1

        public AppAuthService(ILogger<AppAuthService> logger) // MUDANÇA 1
        {
            _logger = logger;
        }

        public async Task SignInUser(Users user, HttpContext httpContext)
        {
            // MUDANÇA 2: Validação de Parâmetros (Fail-Fast)
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "O objeto do usuário não pode ser nulo para o login.");
            }
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext), "O HttpContext é necessário para o login.");
            }

            // MUDANÇA 3: Bloco try-catch para capturar erros inesperados durante o processo de login
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty),
                };

                // No seu Program.cs, o esquema de cookie foi definido como CookieAuthenticationDefaults.AuthenticationScheme
                // É uma boa prática usar a constante em vez de uma string mágica.
                var claimsIdentity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                };

                await httpContext.SignInAsync(
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // MUDANÇA 5: Logging de sucesso (boa prática)
                _logger.LogInformation("Usuário {UserId} - {UserEmail} logado com sucesso.", user.Id, user.Email);
            }
            catch (Exception ex)
            {
                // MUDANÇA 4: Logging detalhado do erro e lançamento de exceção customizada
                _logger.LogError(ex, "Erro inesperado ao tentar realizar o login para o usuário {UserId} - {UserEmail}.", user.Id, user.Email);

                // Lança nossa exceção base, que pode ser capturada por um middleware ou pelo controller
                throw new AppServiceException("Ocorreu um erro inesperado ao tentar realizar o login.", ex);
            }
        }
    }
}