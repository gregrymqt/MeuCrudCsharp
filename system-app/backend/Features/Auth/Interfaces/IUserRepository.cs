using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserRepository
{
    Task<Users?> FindByGoogleIdAsync(string googleId);
    Task<Users?> GetByIdAsync(string id);
    
    // Métodos de escrita (não chamam SaveChanges)
    void Update(Users user);
}
