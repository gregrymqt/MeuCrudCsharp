using System.Security.Claims;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Auth
{
    /// <summary>
    /// Contrato do serviço de autenticação da aplicação.
    /// Responsável por efetuar login e gerar tokens JWT.
    /// </summary>
    public interface IAppAuthService
    {
        /// <summary>
        /// Gera um token JWT assinado para o usuário informado.
        /// </summary>
        /// <param name="user">Usuário para o qual o token será gerado.</param>
        /// <returns>Token JWT assinado.</returns>
        Task<string> GenerateJwtTokenAsync(Users user);

        /// <summary>
        /// Realiza o login do usuário com as credenciais do Google.
        /// </summary>
        /// <param name="googleUserPrincipal"></param>
        /// <param name="httpContext"></param>
        /// <returns>Usuário autenticado.</returns>
        Task<Users> SignInWithGoogleAsync(
            ClaimsPrincipal googleUserPrincipal,
            HttpContext httpContext
        );
    }
}
