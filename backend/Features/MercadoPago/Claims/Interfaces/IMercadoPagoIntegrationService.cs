using System;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;

public interface IMercadoPagoIntegrationService
{
    // Mudança: Adicionado parâmetro 'role' (respondent ou complainant)
    Task<MpClaimSearchResponse> SearchClaimsAsync(string role, int offset = 0, int limit = 30);

    // O resto continua igual, pois ID é único
    Task<List<MpMessageResponse>> GetClaimMessagesAsync(long claimId);

    Task<MpClaimItem?> GetClaimByIdAsync(long claimId);

    Task SendMessageAsync(
        long claimId,
        string message,
        List<string>? attachments = null,
        string receiverRole = "complainant"
    );

    // Novo: Permite escalar para mediação
    Task EscalateToMediationAsync(long claimId);
}
