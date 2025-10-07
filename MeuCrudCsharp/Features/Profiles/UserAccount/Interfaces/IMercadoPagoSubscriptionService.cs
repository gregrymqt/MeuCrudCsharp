using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;

public interface IMercadoPagoSubscriptionService
{
    Task<SubscriptionResponseDto?> GetSubscriptionAsync(string externalSubscriptionId);
    Task UpdateSubscriptionCardAsync(string externalSubscriptionId, string newCardToken);
    Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string externalSubscriptionId, string newStatus);
}