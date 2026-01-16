using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels; // Ajuste conforme seu namespace de ViewModels

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public class AdminClaimService : IAdminClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IMercadoPagoIntegrationService _mpService; // Injeção nova
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminClaimService> _logger;

    private const int PageSize = 10;
    private const string ClaimsCacheVersionKey = "claims_cache_version";

    public AdminClaimService(
        IClaimRepository claimRepository,
        IMercadoPagoIntegrationService mpService, // Recebe o service do MP
        ICacheService cacheService,
        ILogger<AdminClaimService> logger
    )
    {
        _claimRepository = claimRepository;
        _mpService = mpService;
        _cacheService = cacheService;
        _logger = logger;
    }

    // 1. Listagem (Vem do Banco Local - Rápido e com Cache) [cite: 17, 18]
    public async Task<ClaimsIndexViewModel> GetClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page
    )
    {
        var cacheVersion = await GetCacheVersionAsync();
        string cacheKey = $"Claims_v{cacheVersion}_s:{searchTerm}_f:{statusFilter}_p:{page}";

        return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    var (claims, totalCount) = await _claimRepository.GetPaginatedClaimsAsync(
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
                            // CORREÇÃO CS0029: Convertendo Enum para String
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

    // 2. Detalhes com Chat (Vem do MP AO VIVO - Sem Cache longo)
    public async Task<ClaimDetailViewModel> GetClaimDetailsAsync(long localId)
    {
        var localClaim = await _claimRepository.GetByIdAsync(localId);
        if (localClaim == null)
            throw new ResourceNotFoundException("Reclamação não encontrada.");

        List<MpMessageResponse> messages;
        try
        {
            messages = await _mpService.GetClaimMessagesAsync(localClaim.MpClaimId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao buscar mensagens no MP para a claim {ClaimId}",
                localClaim.MpClaimId
            );
            messages = new List<MpMessageResponse>();
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
                    SenderRole = m.SenderRole, // Já é string, não precisa de ToString()
                    Content = m.Message, // Corrigido de .Content para .Message
                    DateCreated = m.DateCreated,
                    Attachments =
                        m.Attachments?.Select(a => a.Filename).ToList() ?? new List<string>(),
                })
                .ToList(),
        };
    }

    // 3. Responder Aluno (Envia para MP)
    public async Task ReplyToClaimAsync(long localId, string messageText)
    {
        var localClaim = await _claimRepository.GetByIdAsync(localId);
        if (localClaim == null)
            throw new ResourceNotFoundException("Reclamação não encontrada.");

        // Envia para o Mercado Pago
        await _mpService.SendMessageAsync(localClaim.MpClaimId, messageText);

        // Opcional: Atualizar status local se necessário
        _logger.LogInformation("Resposta enviada para a claim MP {MpId}", localClaim.MpClaimId);
    }

    private Task<string?> GetCacheVersionAsync()
    {
        return _cacheService.GetOrCreateAsync(
            ClaimsCacheVersionKey,
            () => Task.FromResult(Guid.NewGuid().ToString())
        );
    }
}
