namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserContext
{
    /// <summary>
    /// Obtém o ID do usuário autenticado na requisição atual.
    /// </summary>
    /// <returns>A string representando o ID do usuário.</returns>
    /// <exception cref="AppServiceException">Lançada se o ID do usuário não for encontrado.</exception>
    Task<string> GetCurrentUserId();

    Task<string> GetCurrentEmail();
}
