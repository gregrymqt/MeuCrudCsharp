using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Features.Clients.DTOs;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    // Esta interface agora é o nosso único ponto de contato com a API do Mercado Pago
    public interface IMercadoPagoService
    {
        // --- Métodos de Criação (usados pelo Admin e no checkout do usuário) ---
        Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto planDto);
        Task<SubscriptionResponseDto> CreateSubscriptionAsync(string preapprovalPlanId, string cardId, string payerEmail);

        // --- MÉTODOS ADICIONADOS QUE ESTAVAM FALTANDO ---

        /// <summary>
        /// Busca os detalhes de uma assinatura específica na API do Mercado Pago.
        /// </summary>
        Task<SubscriptionResponseDto> GetSubscriptionAsync(string subscriptionId);

        Task<PlanSearchResponseDto> SearchPlansAsync();

        /// <summary>
        /// Atualiza o cartão de crédito de uma assinatura.
        /// </summary>
        Task<SubscriptionResponseDto> UpdateSubscriptionCardAsync(
            string subscriptionId,
            string cardTokenId
        );

        /// <summary>
        /// Atualiza o status de uma assinatura (ex: para 'cancelled' ou 'authorized').
        /// </summary>
        Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            string newStatus
        );

        /// <summary>
        /// Atualiza o valor de uma assinatura (geralmente usado por Admins).
        /// </summary>
        Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        );

        Task<PlanResponseDto> UpdatePlanAsync(string externalPlanId, UpdatePlanDto updateDto);

        Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName);
        Task<CardResponseDto> SaveCardToCustomerAsync(string customerId, string cardToken);

    }
}
