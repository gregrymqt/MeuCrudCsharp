using System.Security.Claims;
using MeuCrudCsharp.Features.Subscriptions.DTOs;

namespace MeuCrudCsharp.Features.Subscriptions.Interfaces
{
    public interface ISubscriptionService
    {
        // Ação do Usuário
        Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal user
        );

        // Ações do Admin
        Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId);
        Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        );
        Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        );
    }
}
