using System;
using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public class MercadoPagoIntegrationService : MercadoPagoServiceBase, IMercadoPagoIntegrationService
{
    public MercadoPagoIntegrationService(IHttpClientFactory httpClientFactory, ILogger<MercadoPagoIntegrationService> logger)
        : base(httpClientFactory, logger) // [cite: 44, 45]
    {
    }

    public async Task<MpClaimSearchResponse> SearchClaimsAsync(string role, int offset = 0, int limit = 30)
    {
        // Agora injetamos a role na URL 
        var endpoint = $"v1/claims/search?role={role}&offset={offset}&limit={limit}";

        var jsonResponse = await SendMercadoPagoRequestAsync<object>(HttpMethod.Get, endpoint, null);
        return JsonSerializer.Deserialize<MpClaimSearchResponse>(jsonResponse) ?? new MpClaimSearchResponse();
    }

    public async Task EscalateToMediationAsync(long claimId)
    {
        // Endpoint de mediação 
        var endpoint = $"v1/claims/{claimId}/actions/open-dispute";
        await SendMercadoPagoRequestAsync<object>(HttpMethod.Post, endpoint, null);
    }

    public async Task<List<MpMessageResponse>> GetClaimMessagesAsync(long claimId)
    {
        var endpoint = $"v1/claims/{claimId}/messages";

        var jsonResponse = await SendMercadoPagoRequestAsync<object>(HttpMethod.Get, endpoint, null);

        return JsonSerializer.Deserialize<List<MpMessageResponse>>(jsonResponse)
               ?? new List<MpMessageResponse>();
    }

    public async Task SendMessageAsync(long claimId, string message, List<string>? attachments = null, string receiverRole = "complainant")
    {
        // Documentação: POST /v1/claims/{id}/actions/send-message 
        var endpoint = $"v1/claims/{claimId}/actions/send-message";

        var payload = new MpPostMessageRequest
        {
            Message = message,
            ReceiverRole = receiverRole,
            Attachments = attachments
        };

        // Envia usando POST
        await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
    }
}
