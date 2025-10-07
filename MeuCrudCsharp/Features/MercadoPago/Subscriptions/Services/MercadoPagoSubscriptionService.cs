using System.Text.Json;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

public class MercadoPagoSubscriptionService : MercadoPagoServiceBase,IMercadoPagoSubscriptionService // Herda sua classe base com o SendRequest
{
    public MercadoPagoSubscriptionService(IHttpClientFactory httpClient, ILogger<MercadoPagoSubscriptionService> logger)
        : base(httpClient, logger) { }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(SubscriptionWithCardRequestDto payload)
    {
        const string endpoint = "/preapproval";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
               ?? throw new AppServiceException("Falha ao desserializar a resposta de criação de assinatura.");
    }

    public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
    {
        var endpoint = $"/preapproval/{subscriptionId}";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
               ?? throw new AppServiceException("Falha ao desserializar os dados da assinatura.");
    }

    public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(string subscriptionId, UpdateSubscriptionValueDto dto)
    {
        var endpoint = $"/v1/preapproval/{subscriptionId}";
        var payload = new UpdateSubscriptionValueDto(dto.TransactionAmount);
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
               ?? throw new AppServiceException("Falha ao desserializar a resposta de atualização de valor.");
    }

    public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string subscriptionId, UpdateSubscriptionStatusDto dto)
    {
        var endpoint = $"/v1/preapproval/{subscriptionId}";
        var payload = new UpdateSubscriptionStatusDto(dto.Status);
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
               ?? throw new AppServiceException("Falha ao desserializar a resposta de atualização de status.");
    }
}