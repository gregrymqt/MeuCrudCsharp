using MeuCrudCsharp.Models;

// Substitua pelo seu namespace de Models

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserRepository
{
    /// <summary>
    /// Busca um usuário pelo seu GoogleId.
    /// </summary>
    /// <param name="googleId">O ID fornecido pelo provedor Google.</param>
    /// <returns>A entidade do usuário ou null se não for encontrado.</returns>
    Task<Users?> FindByGoogleIdAsync(string googleId);
    
    Task<Users?> GetByIdAsync(string id);

    Task<int> SaveChangesAsync();


}