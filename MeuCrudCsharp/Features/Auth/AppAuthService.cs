using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace MeuCrudCsharp.Features.Auth
{
    public class AppAuthService : IAppAuthService
    {
        public async Task SignInUser(Users user, HttpContext httpContext)
        {
            // Crie as "claims" para o cookie de autenticação local.
            // Inclua apenas o que sua aplicação precisa. O ID do seu banco é a claim mais importante.
            var claims = new List<Claim>
        {
            // Usamos o ID do NOSSO banco de dados, não o do Google, como identificador principal.
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("AvatarUrl", user.AvatarUrl ?? "") // Adiciona claims customizadas se precisar
        };
            
            // Crie a identidade do usuário para o seu esquema de cookie
            var claimsIdentity = new ClaimsIdentity(claims, "MeuEsquemaDeCookie");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // O cookie sobrevive ao fechamento do navegador
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Duração do login
            };

            // Realiza o "login" efetivo, criando o cookie de autenticação criptografado
            await httpContext.SignInAsync(
                "MeuEsquemaDeCookie",
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}
