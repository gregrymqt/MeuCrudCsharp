using MercadoPago.Resource.User;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;

public interface IUserAccountRepository
{
    Task<Users?> GetUserByIdAsync(string userId);
    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);
    Task<IEnumerable<Payments>> GetPaymentHistoryByUserIdAsync(string userId, int pageNumber, int pageSize);
    Task<Payments?> GetPaymentByIdAndUserIdAsync(string userId, string paymentId);
    Task<bool> SaveChangesAsync();
}