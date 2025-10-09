using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages user subscriptions with a payment provider.
    /// </summary>
    public interface ISubscriptionService
    {
        Task<Subscription> CreateSubscriptionAsync(
            string userId,
            string planExternalId,
            string savedCardId,
            string payerEmail,
            string lastFourDigits
        );
        
        Task<Subscription> ActivateSubscriptionFromSinglePaymentAsync(
            string userId,
            Guid planPublicId,
            string paymentId,
            string payerEmail,
            string? lastFourCardDigits
        );

        Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId);

        Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        );

        Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string subscriptionId,
            UpdateSubscriptionStatusDto dto);
        
        
    }
}