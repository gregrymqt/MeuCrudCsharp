// Local: Features/Subscriptions/Interfaces/ISubscriptionService.cs

using System.Security.Claims;
using MeuCrudCsharp.Features.Subscriptions.DTOs;

namespace MeuCrudCsharp.Features.Subscriptions.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionResponseDto> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal users
        );
        Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId);
        Task<SubscriptionResponseDto> UpdateSubscriptionCardAsync(
            string subscriptionId,
            string newCardId
        );
        Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        );
        Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(string subscriptionId, UpdateSubscriptionValueDto dto);

    }
}
