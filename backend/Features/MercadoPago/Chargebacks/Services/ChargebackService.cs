using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels.ChargeBackViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services;

public class ChargebackService : IChargebackService
{
    private readonly IChargebackRepository _chargebackRepository;
    private const int PageSize = 10;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChargebackService> _logger;
    private readonly IMercadoPagoChargebackIntegrationService _mpIntegrationService;

    public ChargebackService(
        IChargebackRepository chargebackRepository,
        ICacheService cacheService,
        ILogger<ChargebackService> logger,
        IMercadoPagoChargebackIntegrationService mpIntegrationService
    )
    {
        _chargebackRepository = chargebackRepository;
        _cacheService = cacheService;
        _logger = logger;
        _mpIntegrationService = mpIntegrationService;
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

    public async Task<ChargebackDetailViewModel> GetChargebackDetailAsync(string chargebackId)
    {
        // Chave de cache para detalhes individuais
        string cacheKey = $"mp_chargeback_detail:{chargebackId}";

        // Usando seu ICacheService
        return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    // 1. Busca na API externa
                    var mpData = await _mpIntegrationService.GetChargebackDetailsFromApiAsync(
                        chargebackId
                    );

                    if (mpData == null)
                        throw new ResourceNotFoundException(
                            $"Chargeback {chargebackId} não encontrado no Mercado Pago."
                        );

                    // 2. Mapeia para o ViewModel (Clean Code: Adapter Pattern implícito)
                    return new ChargebackDetailViewModel
                    {
                        ChargebackId = mpData.Id,
                        Valor = mpData.Amount,
                        Moeda = mpData.Currency,
                        StatusDocumentacao = mpData.DocumentationStatus,
                        CoberturaAplicada = mpData.CoverageApplied,
                        PrecisaDocumentacao = mpData.DocumentationRequired,
                        DataCriacao = mpData.DateCreated,
                        DataLimiteDisputa = mpData.DateDocumentationDeadline,
                        ArquivosEnviados =
                            mpData
                                .Documentation?.Select(doc => new ChargebackFileViewModel
                                {
                                    Tipo = doc.Type,
                                    Url = doc.Url,
                                    NomeArquivo = doc.Description ?? "Arquivo sem descrição",
                                })
                                .ToList() ?? new List<ChargebackFileViewModel>(),
                    };
                },
                TimeSpan.FromMinutes(10)
            ) // Cache de 10 min para não bater na API toda hora
            ?? throw new AppServiceException(
                "Não foi possível recuperar os detalhes do chargeback."
            );
    }
}
