using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Plans.Services
{
    /// <summary>
    /// Implements <see cref="IPlanService"/> to manage the lifecycle of subscription plans.
    /// This service coordinates operations between the local database and the Mercado Pago API.
    /// </summary>
    public class PlanService : MercadoPagoServiceBase, IPlanService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;

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
            HttpClient httpClient,
            ILogger<PlanService> logger
        )
            : base(httpClient, logger)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<List<PlanDto>> GetActivePlansAsync()
        {
            try
            {
                _logger.LogInformation("Buscando planos da API do Mercado Pago.");
                var apiPlans = await FetchPlansFromMercadoPagoAsync();

                // Mapeia os resultados da API para o nosso DTO de exibição
                return apiPlans.Select(MapApiPlanToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Falha ao buscar planos da API. Acionando fallback para o banco de dados local.");
                return await FetchPlansFromDatabaseAsync();
            }
        }

// --- MÉTODOS AUXILIARES ---

        private async Task<IEnumerable<PlanResponseDto>> FetchPlansFromMercadoPagoAsync()
        {
            const string endpoint = "/preapproval_plan/search";
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
            var apiResponse = JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody);

            // Garante que a resposta e os resultados não são nulos antes de continuar
            return apiResponse?.Results?.Where(plan => plan.Status == "active" && plan.AutoRecurring != null)
                   ?? Enumerable.Empty<PlanResponseDto>();
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

        private PlanDto MapApiPlanToDto(PlanResponseDto apiPlan)
        {
            // Nenhuma mudança aqui, a lógica já estava correta!
            bool isAnnual = apiPlan.AutoRecurring!.Frequency == 12 &&
                            apiPlan.AutoRecurring.FrequencyType.Equals("months", StringComparison.OrdinalIgnoreCase);

            return new PlanDto
            {
                Name = apiPlan.Reason,
                Slug = isAnnual ? "anual" : "mensal",
                PriceDisplay = FormatPriceDisplay(apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency),
                BillingInfo = FormatBillingInfo(apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency),
                IsRecommended = isAnnual,
                Features = GetDefaultFeatures()
            };
        }

        private PlanDto MapDbPlanToDto(Plan dbPlan)
        {
            // Nenhuma mudança aqui, a lógica já estava correta!
            bool isAnnual = dbPlan.Frequency == 12 &&
                            dbPlan.FrequencyType.Equals("months", StringComparison.OrdinalIgnoreCase);

            return new PlanDto
            {
                Name = dbPlan.Name,
                Slug = isAnnual ? "anual" : "mensal",
                PriceDisplay = FormatPriceDisplay(dbPlan.TransactionAmount, dbPlan.Frequency),
                BillingInfo = FormatBillingInfo(dbPlan.TransactionAmount, dbPlan.Frequency),
                IsRecommended = isAnnual,
                Features = GetDefaultFeatures()
            };
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
    }
}