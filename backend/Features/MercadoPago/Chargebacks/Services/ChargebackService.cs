using System;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services;

public class ChargebackService : IChargebackService
{
    private readonly IChargebackRepository _chargebackRepository;
    private const int PageSize = 10;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChargebackService> _logger;

    public ChargebackService(
        IChargebackRepository chargebackRepository,
        ICacheService cacheService,
        ILogger<ChargebackService> logger
    )
    {
        _chargebackRepository = chargebackRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ChargebacksIndexViewModel> GetChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page
    )
    {
        string cacheKey = $"Chargebacks_s:{searchTerm}_f:{statusFilter}_p:{page}";

        return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Cache miss para a chave {CacheKey}. Buscando chargebacks do banco de dados.",
                        cacheKey
                    );

                    var (chargebacks, totalCount) =
                        await _chargebackRepository.GetPaginatedChargebacksAsync(
                            searchTerm,
                            statusFilter,
                            page,
                            PageSize
                        );

                    var chargebackSummaries = chargebacks
                        .Select(c => new ChargebackSummaryViewModel
                        {
                            Id = c.ChargebackId.ToString(),
                            Customer = c.User.Name,
                            Amount = c.Amount,
                            Date = c.CreatedAt,
                            Status = c.Status.ToString(),
                            MercadoPagoUrl =
                                $"https://www.mercadopago.com.br/gz/chargebacks/{c.ChargebackId}",
                        })
                        .ToList();

                    var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

                    return new ChargebacksIndexViewModel
                    {
                        Chargebacks = chargebackSummaries,
                        CurrentPage = page,
                        TotalPages = totalPages,
                        SearchTerm = searchTerm,
                        StatusFilter = statusFilter,
                    };
                },
                TimeSpan.FromMinutes(5)
            ) ?? throw new AppServiceException("Erro ao obter chargebacks.");
    }
}
