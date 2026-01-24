using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

/// <summary>
/// Service responsável por operações de Claims do lado ADMINISTRATIVO.
/// Apenas leitura e envio de mensagens para o Mercado Pago (não precisa de UoW).
/// </summary>
public class AdminClaimService(
    IClaimRepository claimRepository,
    IMercadoPagoIntegrationService mpService,
    ICacheService cacheService,
    ILogger<AdminClaimService> logger)
    : IAdminClaimService
{
    private const int PageSize = 10;
    private const string ClaimsCacheVersionKey = "claims_cache_version";

    /// <summary>
    /// Obtém lista paginada de claims com filtros opcionais.
    /// Utiliza cache de 5 minutos para otimizar performance.
    /// </summary>
    public async Task<ClaimsIndexViewModel> GetClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page
    )
    {
        // Validação básica
        if (page < 1)
        {
            logger.LogWarning("Página inválida recebida: {Page}. Usando página 1.", page);
            page = 1;
        }

        var cacheVersion = await GetCacheVersionAsync();
        var cacheKey = $"Claims_v{cacheVersion}_s:{searchTerm}_f:{statusFilter}_p:{page}";

        return await cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var (claims, totalCount) = await claimRepository.GetPaginatedClaimsAsync(
                    searchTerm,
                    statusFilter,
                    page,
                    PageSize
                );

                var claimSummaries = claims
                    .Select(c => new ClaimSummaryViewModel
                    {
                        InternalId = c.Id,
                        MpClaimId = c.MpClaimId,
                        CustomerName = c.User?.Name ?? "Desconhecido",
                        Status = c.Status.ToString(),
                        DateCreated = c.DataCreated,
                        Type = c.Type.ToString(),
                    })
                    .ToList();

                return new ClaimsIndexViewModel
                {
                    Claims = claimSummaries,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                };
            },
            TimeSpan.FromMinutes(5)
        ) ?? new ClaimsIndexViewModel();
    }

    /// <summary>
    /// Obtém detalhes de uma claim específica com mensagens atualizadas do Mercado Pago.
    /// </summary>
    public async Task<ClaimDetailViewModel> GetClaimDetailsAsync(long localId)
    {
        var localClaim = await claimRepository.GetByIdAsync(localId);
        if (localClaim == null)
            throw new ResourceNotFoundException("Reclamação não encontrada.");

        List<MpMessageResponse> messages;
        try
        {
            logger.LogInformation("Buscando mensagens da claim {MpClaimId} na API do Mercado Pago",
                localClaim.MpClaimId);
            messages = await mpService.GetClaimMessagesAsync(localClaim.MpClaimId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao buscar mensagens no MP para a claim {ClaimId}",
                localClaim.MpClaimId
            );
            messages = [];
        }

        return new ClaimDetailViewModel
        {
            InternalId = localClaim.Id,
            MpClaimId = localClaim.MpClaimId,
            Status = localClaim.Status.ToString(),
            Messages = messages
                .Select(m => new ClaimMessageViewModel
                {
                    MessageId = m.Id,
                    SenderRole = m.SenderRole,
                    Content = m.Message,
                    DateCreated = m.DateCreated,
                    Attachments = m.Attachments?.Select(a => a.Filename).ToList() ?? [],
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Envia uma resposta do administrador para uma reclamação.
    /// A mensagem é enviada diretamente para a API do Mercado Pago.
    /// </summary>
    public async Task ReplyToClaimAsync(long localId, string messageText)
    {
        // Validação de entrada
        if (string.IsNullOrWhiteSpace(messageText))
            throw new ArgumentException("Mensagem não pode ser vazia.", nameof(messageText));

        var localClaim = await claimRepository.GetByIdAsync(localId);
        if (localClaim == null)
            throw new ResourceNotFoundException("Reclamação não encontrada.");

        // Envia mensagem para o Mercado Pago
        await mpService.SendMessageAsync(localClaim.MpClaimId, messageText);

        logger.LogInformation("Resposta enviada para a claim MP {MpId}", localClaim.MpClaimId);
    }

    private Task<string?> GetCacheVersionAsync()
    {
        return cacheService.GetOrCreateAsync(
            ClaimsCacheVersionKey,
            () => Task.FromResult(Guid.NewGuid().ToString())
        );
    }
}