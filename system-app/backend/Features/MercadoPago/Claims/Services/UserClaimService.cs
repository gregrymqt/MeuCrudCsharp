using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Models;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

/// <summary>
/// Service responsável por operações de Claims do lado do USUÁRIO/ALUNO.
/// Apenas leitura local e envio de mensagens para o Mercado Pago (não precisa de UoW).
/// </summary>
public class UserClaimService(
    IClaimRepository claimRepository,
    IMercadoPagoIntegrationService mpService,
    IUserContext userContext)
    : IUserClaimService
{
    /// <summary>
    /// Lista todas as reclamações do usuário logado.
    /// </summary>
    public async Task<List<ClaimSummaryViewModel>> GetMyClaimsAsync()
    {
        var userId = userContext.GetCurrentUserId().ToString() 
            ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

        var myClaims = await claimRepository.GetClaimsByUserIdAsync(userId);

        return myClaims
            .Select(c => new ClaimSummaryViewModel
            {
                InternalId = c.Id,
                MpClaimId = c.MpClaimId,
                Status = c.Status.ToString(),
                Type = c.Type.ToString(),
                DateCreated = c.DataCreated,
                IsUrgent = c.Status == InternalClaimStatus.RespondidoPeloVendedor,
            })
            .ToList();
    }

    /// <summary>
    /// Obtém detalhes de uma reclamação específica do usuário logado.
    /// Busca mensagens em tempo real da API do Mercado Pago.
    /// </summary>
    public async Task<ClaimDetailViewModel> GetMyClaimDetailAsync(int internalId)
    {
        var userId = userContext.GetCurrentUserId().ToString();
        var claim = await claimRepository.GetByIdAsync(internalId);

        // Segurança: Impede visualizar reclamação de outro usuário
        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException("Essa reclamação não é sua.");

        // Busca mensagens atualizadas no Mercado Pago
        var messages = await mpService.GetClaimMessagesAsync(claim.MpClaimId);

        return new ClaimDetailViewModel
        {
            InternalId = claim.Id,
            MpClaimId = claim.MpClaimId,
            Status = claim.Status.ToString(),
            Messages = messages
                .Select(m => new ClaimMessageViewModel
                {
                    MessageId = m.Id,
                    SenderRole = m.SenderRole,
                    Content = m.Message,
                    DateCreated = m.DateCreated,
                    Attachments = m.Attachments?.Select(a => a.Filename).ToList() ?? [],
                    IsMe = m.SenderRole == "complainant",
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Envia uma resposta do aluno para uma reclamação.
    /// A mensagem é enviada diretamente para a API do Mercado Pago.
    /// </summary>
    public async Task ReplyAsync(int internalId, string message)
    {
        // Validação de entrada
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem não pode ser vazia.", nameof(message));

        var userId = userContext.GetCurrentUserId().ToString();
        var claim = await claimRepository.GetByIdAsync(internalId);

        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException("Ação não permitida.");

        // Envia mensagem para o Mercado Pago
        await mpService.SendMessageAsync(claim.MpClaimId, message);
    }

    /// <summary>
    /// Solicita mediação do Mercado Pago para uma reclamação (escalar disputa).
    /// </summary>
    public async Task RequestMediationAsync(int internalId)
    {
        var userId = userContext.GetCurrentUserId().ToString();
        var claim = await claimRepository.GetByIdAsync(internalId);

        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException("Ação não permitida.");

        // Escala para mediação na API do Mercado Pago
        await mpService.EscalateToMediationAsync(claim.MpClaimId);
    }
}
