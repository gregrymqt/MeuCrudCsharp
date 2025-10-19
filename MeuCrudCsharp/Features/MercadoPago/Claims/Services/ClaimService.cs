using System;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ClaimService> _logger;
    private const int PageSize = 10;
    private const string ClaimsCacheVersionKey = "claims_cache_version";

    public ClaimService(
        IClaimRepository claimRepository,
        ICacheService cacheService,
        ILogger<ClaimService> logger
    )
    {
        _claimRepository = claimRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

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
                    _logger.LogInformation(
                        "Cache miss para a chave {CacheKey}. Buscando claims do banco de dados.",
                        cacheKey
                    );

                    var (claims, totalCount) = await _claimRepository.GetPaginatedClaimsAsync(
                        searchTerm,
                        statusFilter,
                        page,
                        PageSize
                    );

                    var claimSummaries = claims
                        .Select(c => new ClaimSummaryViewModel
                        {
                            Id = c.Id,
                            OrderId = c.ClaimId,
                            CustomerName = c.User.Name,
                            Status = c.Status.ToString(),
                            DateCreated = c.DataCreated,
                            Reason = c.Type,
                        })
                        .ToList();

                    var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

                    return new ClaimsIndexViewModel
                    {
                        Claims = claimSummaries,
                        CurrentPage = page,
                        TotalPages = totalPages,
                        SearchTerm = searchTerm,
                        StatusFilter = statusFilter,
                    };
                },
                TimeSpan.FromMinutes(5)
            ) ?? throw new AppServiceException("Erro ao obter reclamações.");
    }

    public async Task UpdateClaimStatusAsync(long id, string newStatus)
    {
        var claim = await _claimRepository.GetByIdAsync(id);
        if (claim is null)
        {
            throw new ResourceNotFoundException("Reclamação não encontrada.");
        }

        await _claimRepository.UpdateClaimStatusAsync(claim, newStatus);
        await _cacheService.InvalidateCacheByKeyAsync(ClaimsCacheVersionKey);
        _logger.LogInformation(
            "Cache de claims invalidado devido à atualização do status da claim ID {ClaimId}.",
            id
        );
    }

    private Task<string?> GetCacheVersionAsync()
    {
        return _cacheService.GetOrCreateAsync(
            ClaimsCacheVersionKey,
            () => Task.FromResult(Guid.NewGuid().ToString())
        );
    }
}
