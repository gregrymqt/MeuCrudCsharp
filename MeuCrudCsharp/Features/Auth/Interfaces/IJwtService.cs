namespace MeuCrudCsharp.Features.Auth.Interfaces;

using Models; // Substitua pelo seu namespace de Models
using System.Threading.Tasks;

public interface IJwtService
{
    /// <summary>
    /// Gera um token JWT para o usuário informado.
    /// </summary>
    /// <param name="user">A entidade do usuário para o qual o token será gerado.</param>
    /// <returns>A string do token JWT assinado.</returns>
    Task<string> GenerateJwtTokenAsync(Users user);
}