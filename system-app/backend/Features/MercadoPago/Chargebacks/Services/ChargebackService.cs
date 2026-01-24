using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using static MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels.ChargeBackViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services;

/// <summary>
/// Service responsável por operações de LEITURA de Chargebacks.
/// Para operações de escrita (Create/Update), veja ChargeBackNotificationService.
/// </summary>
public class ChargebackService(
    IChargebackRepository chargebackRepository,
    ICacheService cacheService,
    ILogger<ChargebackService> logger,
    IMercadoPagoChargebackIntegrationService mpIntegrationService)
    : IChargebackService
{
    private const int PageSize = 10;

    /// <summary>
    /// Obtém lista paginada de chargebacks com filtros opcionais.
    /// Utiliza cache de 5 minutos para otimizar performance.
    /// </summary>
    public async Task<ChargebacksIndexViewModel> GetChargebacksAsync(
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

        var cacheKey = $"Chargebacks_s:{searchTerm}_f:{statusFilter}_p:{page}";

        return await cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    logger.LogInformation(
                        "Cache miss para a chave {CacheKey}. Buscando chargebacks do banco de dados.",
                        cacheKey
                    );

                    var (chargebacks, totalCount) =
                        await chargebackRepository.GetPaginatedChargebacksAsync(
                            searchTerm,
                            statusFilter,
                            page,
                            PageSize
                        );

                    var chargebackSummaries = chargebacks
                        .Select(c => new ChargebackSummaryViewModel
                        {
                            Id = c.ChargebackId.ToString(),
                            Customer = c.User?.Name,
                            Amount = c.Amount,
                            Date = c.CreatedAt,
                            Status = (int)c.Status,
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

    /// <summary>
    /// Obtém detalhes completos de um chargeback específico da API do Mercado Pago.
    /// Utiliza cache de 10 minutos para reduzir chamadas à API externa.
    /// </summary>
    public async Task<ChargebackDetailViewModel> GetChargebackDetailAsync(string chargebackId)
    {
        // Validação de entrada
        if (string.IsNullOrWhiteSpace(chargebackId))
        {
            throw new ArgumentException("ID do chargeback não pode ser vazio.", nameof(chargebackId));
        }

        // Chave de cache para detalhes individuais
        var cacheKey = $"mp_chargeback_detail:{chargebackId}";

        // Usando seu ICacheService
        return await cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    logger.LogInformation("Buscando detalhes do chargeback {Id} na API do Mercado Pago", chargebackId);
                    
                    // 1. Busca na API externa
                    var mpData = await mpIntegrationService.GetChargebackDetailsFromApiAsync(
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
                                .ToList() ?? [],
                    };
                },
                TimeSpan.FromMinutes(10) // Cache de 10 min para não bater na API toda hora
            )
            ?? throw new AppServiceException(
                "Não foi possível recuperar os detalhes do chargeback."
            );
    }
}
