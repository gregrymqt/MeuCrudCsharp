using System.Text.Json;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

// Local: Features/MercadoPago/Subscriptions/Services/MercadoPagoSubscriptionService.cs
public class MercadoPagoSubscriptionService
    : MercadoPagoServiceBase,
        IMercadoPagoSubscriptionService
{
    public MercadoPagoSubscriptionService(
        IHttpClientFactory httpClient,
        ILogger<MercadoPagoSubscriptionService> logger
    )
        : base(httpClient, logger) { }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
        CreateSubscriptionDto payload
    )
    {
        const string endpoint = "/preapproval"; // Endpoint de criação não costuma ter /v1
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
            ?? throw new AppServiceException(
                "Falha ao desserializar a resposta de criação de assinatura."
            );
    }

    public async Task<SubscriptionResponseDto?> GetSubscriptionByIdAsync(string subscriptionId)
    {
        var endpoint = $"/preapproval/{subscriptionId}"; // Padronizado sem /v1 para GET
        var responseBody = await SendMercadoPagoRequestAsync(
            HttpMethod.Get,
            endpoint,
            (object?)null
        );
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);
    }

    public async Task UpdateSubscriptionCardAsync(string subscriptionId, string newCardToken)
    {
        // PADRONIZADO: Usando o endpoint com /v1, que é mais comum para updates.
        var endpoint = $"/v1/preapproval/{subscriptionId}";
        var payload = new { card_token_id = newCardToken }; // Monta o payload esperado pela API
        await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
    }

    public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
        string subscriptionId,
        UpdateSubscriptionValueDto dto
    )
    {
        var endpoint = $"/v1/preapproval/{subscriptionId}";
        var payload = new { auto_recurring = new { transaction_amount = dto.TransactionAmount } };
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
            ?? throw new AppServiceException(
                "Falha ao desserializar a resposta de atualização de valor."
            );
    }

    public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
        string subscriptionId,
        UpdateSubscriptionStatusDto dto
    )
    {
        var endpoint = $"/v1/preapproval/{subscriptionId}";
        var payload = new { status = dto.Status };
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
            ?? throw new AppServiceException(
                "Falha ao desserializar a resposta de atualização de status."
            );
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        // Implementação do método de cancelamento para o rollback
        await UpdateSubscriptionStatusAsync(
            subscriptionId,
            new UpdateSubscriptionStatusDto("cancelled")
        );
    }
}
