using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserRepository
{
    Task<Users?> FindByGoogleIdAsync(string googleId);

    Task<Users?> GetByIdAsync(string id);
    
    // SaveChangesAsync removido - UnitOfWork é responsável por isso
}
