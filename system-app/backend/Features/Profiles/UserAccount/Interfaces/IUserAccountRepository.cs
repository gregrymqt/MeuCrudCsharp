using MercadoPago.Resource.User;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;

public interface IUserAccountRepository
{
    Task<Users?> GetUserByIdAsync(string userId);
    // SaveChangesAsync removido - UnitOfWork é responsável por isso
}
