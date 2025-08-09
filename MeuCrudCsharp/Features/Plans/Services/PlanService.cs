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

        /// <inheritdoc />
        /// <remarks>
        /// This method employs an API-first strategy. It first attempts to fetch the latest plan information
        /// directly from the Mercado Pago API. If the API call fails for any reason, it gracefully
        /// falls back to retrieving the last known active plans from the local database.
        /// </remarks>
        public async Task<List<PlanDto>> GetActivePlansAsync()
        {
            try
            {
                _logger.LogInformation("Buscando planos da API do Mercado Pago.");

                const string endpoint = "/preapproval_plan/search";
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Get,
                    endpoint,
                    (object?)null
                );

                var apiPlans = JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody);

                return apiPlans!
                    .Results.Where(plan => plan.AutoRecurring != null)
                    .Select(plan => new PlanDto
                    {
                        Name = plan.Reason,
                        Slug =
                            plan.AutoRecurring!.FrequencyType.ToLower() == "months"
                                ? "mensal"
                                : "anual",
                        PriceDisplay = FormatPriceDisplay(
                            plan.AutoRecurring.TransactionAmount,
                            plan.AutoRecurring.FrequencyType
                        ),
                        BillingInfo = FormatBillingInfo(
                            plan.AutoRecurring.TransactionAmount,
                            plan.AutoRecurring.FrequencyType
                        ),
                        IsRecommended = plan.AutoRecurring.FrequencyType.ToLower() == "years",
                        Features = new List<string>
                        {
                            "Acesso a todos os cursos",
                            "Vídeos novos toda semana",
                            "Suporte via comunidade",
                            "Cancele quando quiser",
                        },
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Falha ao buscar planos da API. Acionando fallback para o banco de dados local."
                );

                try
                {
                    var plansFromDb = await _context
                        .Plans.AsNoTracking()
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.TransactionAmount)
                        .ToListAsync();

                    // Mapeia a entidade do banco para o DTO de exibição
                    return plansFromDb
                        .Select(plan => new PlanDto
                        {
                            Name = plan.Name,
                            Slug = plan.FrequencyType.ToLower() == "months" ? "mensal" : "anual",
                            PriceDisplay = FormatPriceDisplay(
                                plan.TransactionAmount,
                                plan.FrequencyType
                            ),
                            BillingInfo = FormatBillingInfo(
                                plan.TransactionAmount,
                                plan.FrequencyType
                            ),
                            IsRecommended = plan.FrequencyType.ToLower() == "years",
                            Features = new List<string>
                            {
                                "Acesso a todos os cursos",
                                "Vídeos novos toda semana",
                                "Suporte via comunidade",
                                "Cancele quando quiser",
                            },
                        })
                        .ToList();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(
                        dbEx,
                        "Falha crítica: A API e o banco de dados falharam ao buscar os planos."
                    );
                    throw new AppServiceException(
                        "Não foi possível carregar os planos de nenhuma fonte.",
                        dbEx
                    );
                }
            }
        }

        /// <summary>
        /// Formats the price for display on the UI.
        /// </summary>
        /// <param name="amount">The transaction amount.</param>
        /// <param name="frequencyType">The frequency type (e.g., "years", "months").</param>
        /// <returns>A formatted price string (e.g., "R$ 41,58" or "R$ 499,00").</returns>
        private string FormatPriceDisplay(decimal amount, string frequencyType)
        {
            if (frequencyType.ToLower() == "years")
            {
                var monthlyPrice = amount / 12;
                return $"R$ {monthlyPrice:F2}".Replace('.', ',');
            }
            return $"R$ {amount:F2}".Replace('.', ',');
        }

        /// <summary>
        /// Formats the billing information for display on the UI.
        /// </summary>
        /// <param name="amount">The transaction amount.</param>
        /// <param name="frequencyType">The frequency type (e.g., "years", "months").</param>
        /// <returns>A formatted billing info string (e.g., "Cobrado R$ 499,00 anualmente").</returns>
        private string FormatBillingInfo(decimal amount, string frequencyType)
        {
            if (frequencyType.ToLower() == "years")
            {
                return $"Cobrado R$ {amount:F2} anualmente".Replace('.', ',');
            }
            return "&nbsp;"; // Return a non-breaking space to maintain alignment in HTML.
        }

        /// <inheritdoc />
        public async Task<Plan> CreatePlanAsync(CreatePlanDto createDto)
        {
            try
            {
                const string endpoint = "/preapproval_plan";
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Post,
                    endpoint,
                    createDto
                );
                var mpPlanResponse = JsonSerializer.Deserialize<PlanResponseDto>(responseBody);

                var newPlan = new Plan
                {
                    ExternalPlanId = mpPlanResponse.Id,
                    Name = mpPlanResponse.Reason,
                    TransactionAmount = mpPlanResponse.AutoRecurring.TransactionAmount,
                    CurrencyId = mpPlanResponse.AutoRecurring.CurrencyId,
                    Frequency = mpPlanResponse.AutoRecurring.Frequency,
                    FrequencyType = mpPlanResponse.AutoRecurring.FrequencyType,
                    IsActive = true,
                };

                _context.Plans.Add(newPlan);
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation(
                    "Plano '{PlanName}' criado com sucesso no MP e salvo localmente.",
                    newPlan.Name
                );
                return newPlan;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar criar o plano '{PlanName}'.",
                    createDto.Reason
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar o plano '{PlanName}'.",
                    createDto.Reason
                );
                throw new AppServiceException(
                    "Ocorreu um erro em nosso sistema ao criar o plano.",
                    ex
                );
            }
        }

        /// <inheritdoc />
        public async Task<Plan> UpdatePlanAsync(string externalPlanId, UpdatePlanDto updateDto)
        {
            try
            {
                var endpoint = $"/preapproval_plan/{externalPlanId}";
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, updateDto);

                var localPlan = await _context.Plans.FirstOrDefaultAsync(p =>
                    p.ExternalPlanId == externalPlanId
                );
                if (localPlan == null)
                    throw new ResourceNotFoundException(
                        $"Plano com ID externo {externalPlanId} não encontrado localmente para atualização."
                    );

                localPlan.Name = updateDto.Reason;
                localPlan.TransactionAmount = updateDto.TransactionAmount;
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation("Plano {PlanId} atualizado com sucesso.", externalPlanId);

                return localPlan;
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar atualizar o plano {PlanId}.",
                    externalPlanId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao atualizar o plano {PlanId}.",
                    externalPlanId
                );
                throw new AppServiceException(
                    "Ocorreu um erro em nosso sistema ao atualizar o plano.",
                    ex
                );
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// This performs a "soft delete". In Mercado Pago, plans are not deleted but are instead
        /// deactivated by setting their status to 'cancelled'. Locally, the 'IsActive' flag is set to false.
        /// </remarks>
        public async Task DeletePlanAsync(string externalPlanId)
        {
            try
            {
                var endpoint = $"/preapproval_plan/{externalPlanId}";
                var payload = new { status = "cancelled" };
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);

                var localPlan = await _context.Plans.FirstOrDefaultAsync(p =>
                    p.ExternalPlanId == externalPlanId
                );
                if (localPlan != null)
                {
                    localPlan.IsActive = false;
                    await _context.SaveChangesAsync();
                }

                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation("Plano {PlanId} desativado com sucesso.", externalPlanId);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar desativar o plano {PlanId}.",
                    externalPlanId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao desativar o plano {PlanId}.",
                    externalPlanId
                );
                throw new AppServiceException(
                    "Ocorreu um erro em nosso sistema ao desativar o plano.",
                    ex
                );
            }
        }
    }
}
