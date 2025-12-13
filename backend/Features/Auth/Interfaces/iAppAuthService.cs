using System.Security.Claims;
using MeuCrudCsharp.Models; // Substitua pelo seu namespace

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IAppAuthService
{
    /// <summary>
    /// Realiza o login ou registro de um usuário através do provedor Google.
    /// </summary>
    /// <returns>A entidade do usuário logado ou criado.</returns>
    Task<Users> SignInWithGoogleAsync(ClaimsPrincipal googleUserPrincipal, HttpContext httpContext);
}
