using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Services
{
    /// <summary>
    /// Implements <see cref="IPlanService"/> to manage the lifecycle of subscription plans.
    /// This service coordinates operations between the local database and the Mercado Pago API.
    /// </summary>
    public class PlanService : MercadoPagoServiceBase, IPlanService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;
        private const string ApiPlansCacheKey = "ActiveApiPlans";
        private const string DbPlansCacheKey = "ActiveDbPlans";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
        public PlanService(
            ApiDbContext context,
            ICacheService cacheService,
            IHttpClientFactory httpClient,
            ILogger<PlanService> logger
        )
            : base(httpClient, logger)
        {
            _context = context;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Busca os planos ativos diretamente do banco de dados local.
        /// Este é o método principal para ser usado por usuários comuns para performance.
        /// </summary>
        /// <returns>Uma lista de PlanDto.</returns>
        public async Task<List<PlanDto>> GetActiveDbPlansAsync()
        {
            _logger.LogInformation("Buscando planos ativos do banco de dados (com cache).");

            var cachedPlans = await _cacheService.GetOrCreateAsync(
                DbPlansCacheKey,
                FetchPlansFromDatabaseAsync, // A função original já retorna Task<List<PlanDto>>
                TimeSpan.FromMinutes(15) // Cache mais longo para dados que mudam com menos frequência.
            );

            return cachedPlans ?? new List<PlanDto>();
        }

        /// <summary>
        /// Busca os planos ativos diretamente da API do Mercado Pago.
        /// Este método deve ser restrito a administradores para evitar rate limiting.
        /// </summary>
        /// <returns>Uma lista de PlanDto mapeada da resposta da API.</returns>
        public async Task<List<PlanDto>> GetActiveApiPlansAsync()
        {
            _logger.LogInformation("Buscando planos ativos da API (com cache).");

            // Utiliza o GetOrCreateAsync para buscar do cache ou, se não encontrar,
            // executa a função para buscar da API e depois salva o resultado no cache.
            var cachedPlans = await _cacheService.GetOrCreateAsync(
                ApiPlansCacheKey,
                async () =>
                {
                    // A lógica original de busca e mapeamento fica aqui dentro.
                    var apiPlans = await FetchPlansFromMercadoPagoAsync();
                    return apiPlans.ToList();
                },
                TimeSpan.FromMinutes(5) // Cache mais curto para dados que podem mudar com mais frequência.
            );

            // Retorna a lista do cache ou uma lista vazia se tudo falhar.
            return cachedPlans ?? new List<PlanDto>();
        }

// --- MÉTODOS AUXILIARES ---

// Este método agora é o único responsável por buscar da API E mapear os resultados.
        private async Task<IEnumerable<PlanDto>> FetchPlansFromMercadoPagoAsync()
        {
            const string endpoint = "/preapproval_plan/search";
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
            var apiResponse = JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody);

            // Usamos 'PlanResponseDto' aqui, que é o tipo que vem da API
            var activePlansFromApi =
                apiResponse?.Results?.Where(plan => plan.Status == "active" && plan.AutoRecurring != null)
                ?? Enumerable.Empty<PlanResponseDto>();

            var planResponseDtos = activePlansFromApi.ToList();
            if (!planResponseDtos.Any())
            {
                return Enumerable.Empty<PlanDto>();
            }

            var mappingTasks = planResponseDtos.Select(MapApiPlanToDto);

            // 2. Aguarde todas as tarefas terminarem em paralelo
            var mappedPlans = await Task.WhenAll(mappingTasks);

            // 3. Filtre qualquer resultado nulo (planos que não foram encontrados no banco) e retorne a lista
            return mappedPlans.Where(p => p != null).ToList()!;
        }

        private async Task<List<PlanDto>> FetchPlansFromDatabaseAsync()
        {
            try
            {
                var plansFromDb = await _context.Plans
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.TransactionAmount)
                    .ToListAsync();

                // Mapeia as entidades do banco para o nosso DTO de exibição
                return plansFromDb.Select(MapDbPlanToDto).ToList();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Falha crítica: A API e o banco de dados falharam ao buscar os planos.");
                throw new AppServiceException("Não foi possível carregar os planos de nenhuma fonte.", dbEx);
            }
        }

        // Mude a assinatura para async Task<PlanDto?>
        private async Task<PlanDto?> MapApiPlanToDto(PlanResponseDto apiPlan)
        {
            var localPlan = await GetPlanByExternalIdAsync(apiPlan.Id!);

            if (localPlan == null)
            {
                _logger.LogWarning(
                    "Um plano com ExternalPlanId '{ExternalId}' foi encontrado no Mercado Pago, mas não existe no banco de dados local.",
                    apiPlan.Id);
                return null;
            }

            bool isAnnual = apiPlan.AutoRecurring!.Frequency == 12 &&
                            String.Equals(apiPlan.AutoRecurring.FrequencyType, "months",
                                StringComparison.OrdinalIgnoreCase);

            return new PlanDto
            (
                localPlan.PublicId.ToString(),
                apiPlan.Reason,
                isAnnual ? "anual" : "mensal",
                FormatPriceDisplay(apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency),
                FormatBillingInfo(apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency),
                GetDefaultFeatures(),
                isAnnual
            );
        }

        private PlanDto MapDbPlanToDto(Plan dbPlan)
        {
            bool isAnnual = dbPlan.Frequency == 12 &&
                            String.Equals(dbPlan.FrequencyType, "months", StringComparison.OrdinalIgnoreCase);

            return new PlanDto
            (
                dbPlan.PublicId.ToString(),
                dbPlan.Name,
                isAnnual ? "anual" : "mensal",
                FormatPriceDisplay(dbPlan.TransactionAmount, dbPlan.Frequency),
                FormatBillingInfo(dbPlan.TransactionAmount, dbPlan.Frequency),
                GetDefaultFeatures(),
                isAnnual
            );
        }

// Método para evitar repetição da lista de features
        private List<string> GetDefaultFeatures() => new List<string>
        {
            "Acesso a todos os cursos",
            "Vídeos novos toda semana",
            "Suporte via comunidade",
            "Cancele quando quiser",
        };

        /// <summary>
        /// Formats the price for display on the UI.
        /// </summary>
        /// <param name="amount">The transaction amount.</param>
        /// <param name="frequency">The frequency type (e.g., "years", "months").</param>
        /// <returns>A formatted price string (e.g., "R$ 41,58" or "R$ 499,00").</returns>
        private string FormatPriceDisplay(decimal amount, int frequency)
        {
            // Se for anual (frequência 12), calculamos o valor mensal equivalente para exibição.
            if (frequency == 12)
            {
                var monthlyPrice = amount / 12;
                return $"R$ {monthlyPrice:F2}".Replace('.', ',');
            }

            // Se for mensal (frequência 1), apenas formatamos o valor.
            return $"R$ {amount:F2}".Replace('.', ',');
        }

        /// <summary>
        /// Formats the billing information for display on the UI.
        /// </summary>
        /// <param name="amount">The transaction amount.</param>
        /// <param name="frequency">The frequency type (e.g., "years", "months").</param>
        /// <returns>A formatted billing info string (e.g., "Cobrado R$ 499,00 anualmente").</returns>
        private string FormatBillingInfo(decimal amount, int frequency)
        {
            // A lógica agora verifica se a frequência é de 12 (anual)
            if (frequency == 12)
            {
                return $"Cobrado R$ {amount:F2} anualmente".Replace('.', ',');
            }

            // Para qualquer outra frequência (como 1 mês), não mostramos nada.
            return "&nbsp;";
        }

        public async Task<Plan?> GetPlanByExternalIdAsync(string externalId)
        {
            return await _context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive && p.ExternalPlanId == externalId);
        }
    }
}