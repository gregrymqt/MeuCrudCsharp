using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces
{
    public interface IUserAccountService
    {
        // Métodos para obter informações
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
        Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(Guid userId);
        Task<IEnumerable<Models.Payments>> GetUserPaymentHistoryAsync(Guid userId);

        // Métodos para realizar ações
        Task<bool> UpdateSubscriptionCardAsync(Guid userId, string newCardToken);
        Task<bool> CancelSubscriptionAsync(Guid userId);
        Task<bool> ReactivateSubscriptionAsync(Guid userId);
    }
}
