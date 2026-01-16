using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using MeuCrudCsharp.Models;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public class UserClaimService : IUserClaimService
{
    private readonly IClaimRepository _claimRepository; // Seu banco local
    private readonly IMercadoPagoIntegrationService _mpService;
    private readonly IUserContext _userContext; // Para pegar o ID do aluno logado

    public UserClaimService(
        IClaimRepository claimRepository,
        IMercadoPagoIntegrationService mpService,
        IUserContext userContext
    )
    {
        _claimRepository = claimRepository;
        _mpService = mpService;
        _userContext = userContext;
    }

    // 1. Minhas Reclamações
    public async Task<List<ClaimSummaryViewModel>> GetMyClaimsAsync()
    {
        var userId = _userContext.GetCurrentUserId().ToString() ?? throw new UnauthorizedAccessException();

        var myClaims = await _claimRepository.GetClaimsByUserIdAsync(userId);

        return myClaims
            .Select(c => new ClaimSummaryViewModel
            {
                InternalId = c.Id,
                MpClaimId = c.MpClaimId,
                Status = c.Status.ToString(),
                // CORREÇÃO 1: ToString() no Enum Type
                Type = c.Type.ToString(),
                DateCreated = c.DataCreated,
                // CORREÇÃO 2: Nome correto do Enum (InternalClaimStatus)
                IsUrgent = c.Status == InternalClaimStatus.RespondidoPeloVendedor,
            })
            .ToList();
    }

    // 2. Detalhes (O Aluno vendo o chat)
    public async Task<ClaimDetailViewModel> GetMyClaimDetailAsync(int internalId)
    {
        var userId = _userContext.GetCurrentUserId().ToString();
        var claim = await _claimRepository.GetByIdAsync(internalId);

        // SEGURANÇA: Impede ver reclamação de outro aluno
        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException("Essa reclamação não é sua.");

        // Busca mensagens no MP
        var messages = await _mpService.GetClaimMessagesAsync(claim.MpClaimId);

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
                    Content = m.Message, // [cite: 8]
                    DateCreated = m.DateCreated,
                    Attachments = m.Attachments?.Select(a => a.Filename).ToList() ?? new(),
                    IsMe = m.SenderRole == "complainant",
                })
                .ToList(),
        };
    }

    // 3. Aluno Responde
    public async Task ReplyAsync(int internalId, string message)
    {
        var userId = _userContext.GetCurrentUserId().ToString();
        var claim = await _claimRepository.GetByIdAsync(internalId);

        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException("Ação não permitida.");

        // Envia mensagem
        await _mpService.SendMessageAsync(claim.MpClaimId, message);
    }

    // 4. Aluno pede Mediação (Escalar)
    public async Task RequestMediationAsync(int internalId)
    {
        var userId = _userContext.GetCurrentUserId().ToString();
        var claim = await _claimRepository.GetByIdAsync(internalId);

        if (claim == null || claim.UserId != userId)
            throw new UnauthorizedAccessException();

        // Chama endpoint de disputa
        await _mpService.EscalateToMediationAsync(claim.MpClaimId);
    }
}
