using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

public interface IMercadoPagoSubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(SubscriptionWithCardRequestDto payload);
    Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId);
    Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(string subscriptionId, UpdateSubscriptionValueDto dto);
    Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string subscriptionId, UpdateSubscriptionStatusDto dto);
}