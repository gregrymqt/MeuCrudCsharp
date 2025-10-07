using System.Text.Json;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services;

public class MercadoPagoSubscriptionService : MercadoPagoServiceBase, IMercadoPagoSubscriptionService
{
    public MercadoPagoSubscriptionService(IHttpClientFactory httpClient, ILogger<MercadoPagoSubscriptionService> logger)
        : base(httpClient, logger) { }

    public async Task<SubscriptionResponseDto?> GetSubscriptionAsync(string externalSubscriptionId)
    {
        var endpoint = $"/preapproval/{externalSubscriptionId}";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);
    }

    public async Task UpdateSubscriptionCardAsync(string externalSubscriptionId, string newCardToken)
    {
        var endpoint = $"/preapproval/{externalSubscriptionId}";
        var payload = new UpdateCardTokenDto { NewCardToken = newCardToken };
        await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
    }

    public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string externalSubscriptionId, string newStatus)
    {
        var endpoint = $"/preapproval/{externalSubscriptionId}";
        var payload = new { status = newStatus };
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        var subscriptionResponse = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

        return subscriptionResponse 
               ?? throw new Exception("Falha ao deserializar a resposta do Mercado Pago após a atualização.");
    }
}