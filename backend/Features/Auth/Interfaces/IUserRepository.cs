using MeuCrudCsharp.Models;

// Substitua pelo seu namespace de Models

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserRepository
{
    Task<Users?> FindByGoogleIdAsync(string googleId);
    
    Task<Users?> GetByIdAsync(string id);

    Task<int> SaveChangesAsync();

    Task<Users?> GetUserWithDetailsAsync(string userId);

}