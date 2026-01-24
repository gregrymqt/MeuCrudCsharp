using System;
using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public class MercadoPagoIntegrationService(
    IHttpClientFactory httpClientFactory,
    ILogger<MercadoPagoIntegrationService> logger)
    : MercadoPagoServiceBase(httpClientFactory, logger), IMercadoPagoIntegrationService
{
    private const string BaseEndpoint = "post-purchase/v1/claims";

    // [cite: 44, 45]

    public async Task<MpClaimSearchResponse> SearchClaimsAsync(
        string role,
        int offset = 0,
        int limit = 30
    )
    {
        // Agora injetamos a role na URL
        var endpoint = $"{BaseEndpoint}/search?role={role}&offset={offset}&limit={limit}";

        var jsonResponse = await SendMercadoPagoRequestAsync<object>(
            HttpMethod.Get,
            endpoint,
            null
        );
        return JsonSerializer.Deserialize<MpClaimSearchResponse>(jsonResponse)
            ?? new MpClaimSearchResponse();
    }

    public async Task<MpClaimItem?> GetClaimByIdAsync(long claimId)
    {
        var endpoint = $"{BaseEndpoint}/{claimId}";

        var jsonResponse = await SendMercadoPagoRequestAsync<object>(
            HttpMethod.Get,
            endpoint,
            null
        );

        return JsonSerializer.Deserialize<MpClaimItem>(jsonResponse);
    }

    public async Task EscalateToMediationAsync(long claimId)
    {
        // Endpoint de mediação
        var endpoint = $"{BaseEndpoint}/{claimId}/actions/open-dispute";
        await SendMercadoPagoRequestAsync<object>(HttpMethod.Post, endpoint, null);
    }

    public async Task<List<MpMessageResponse>> GetClaimMessagesAsync(long claimId)
    {
        var endpoint = $"{BaseEndpoint}/{claimId}/messages";

        var jsonResponse = await SendMercadoPagoRequestAsync<object>(
            HttpMethod.Get,
            endpoint,
            null
        );

        return JsonSerializer.Deserialize<List<MpMessageResponse>>(jsonResponse)
            ?? [];
    }

    public async Task SendMessageAsync(
        long claimId,
        string message,
        List<string>? attachments = null,
        string receiverRole = "complainant"
    )
    {
        // Documentação: POST /v1/claims/{id}/actions/send-message
        var endpoint = $"{BaseEndpoint}/{claimId}/actions/send-message";

        var payload = new MpPostMessageRequest
        {
            Message = message,
            ReceiverRole = receiverRole,
            Attachments = attachments,
        };

        // Envia usando POST
        await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
    }
}
