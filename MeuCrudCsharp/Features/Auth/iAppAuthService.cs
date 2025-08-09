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
        /// Realiza o login do usuário usando cookies e emite um token JWT armazenado em cookie HttpOnly.
        /// </summary>
        /// <param name="user">Usuário autenticado.</param>
        /// <param name="httpContext">Contexto HTTP atual.</param>
        Task SignInUser(Users user, HttpContext httpContext);

        /// <summary>
        /// Gera um token JWT assinado para o usuário informado.
        /// </summary>
        /// <param name="user">Usuário para o qual o token será gerado.</param>
        /// <returns>Token JWT assinado.</returns>
        Task<string> GenerateJwtTokenAsync(Users user);
    }
}
