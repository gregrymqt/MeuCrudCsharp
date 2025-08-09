using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base; // Importando a classe base
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Plans.Services
{
    // --- CORREÇÃO: Herda da classe base ---
    public class PlanService : MercadoPagoServiceBase, IPlanService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService; // Descomente quando o serviço de cache estiver implementado

        // --- CORREÇÃO: Construtor ajustado ---
        public PlanService(
            ApiDbContext context,
            ICacheService cacheService,
            HttpClient httpClient,
            ILogger<PlanService> logger
        )
            : base(httpClient, logger) // Passa as dependências para a classe base
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<List<PlanDto>> GetActivePlansAsync()
        {
            // Estratégia API-First: Tenta buscar da API do Mercado Pago primeiro
            try
            {
                _logger.LogInformation("Buscando planos da API do Mercado Pago.");

                const string endpoint = "/preapproval_plan/search";
                // --- CORREÇÃO: Chama o método da classe base ---
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
                // ESTRATÉGIA DE FALLBACK
                _logger.LogWarning(
                    ex,
                    "Falha ao buscar planos da API. Acionando fallback para o banco de dados local."
                );

                // MUDANÇA: Adicionando try-catch ao fallback
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

        // Métodos auxiliares para formatação, agora reutilizáveis
        private string FormatPriceDisplay(decimal amount, string frequencyType)
        {
            if (frequencyType.ToLower() == "years")
            {
                var monthlyPrice = amount / 12;
                return $"R$ {monthlyPrice:F2}".Replace('.', ',');
            }
            return $"R$ {amount:F2}".Replace('.', ',');
        }

        private string FormatBillingInfo(decimal amount, string frequencyType)
        {
            if (frequencyType.ToLower() == "years")
            {
                return $"Cobrado R$ {amount:F2} anualmente".Replace('.', ',');
            }
            return "&nbsp;"; // Retorna um espaço para manter o alinhamento
        }

        public async Task<Plan> CreatePlanAsync(CreatePlanDto createDto)
        {
            try
            {
                const string endpoint = "/preapproval_plan";
                // --- CORREÇÃO: Chama o método da classe base ---
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
            catch (ExternalApiException ex) // Erro vindo do MercadoPagoService
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar criar o plano '{PlanName}'.",
                    createDto.Reason
                );
                throw; // Relança a exceção para o controller tratar
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

        public async Task<Plan> UpdatePlanAsync(string externalPlanId, UpdatePlanDto updateDto)
        {
            try
            {
                var endpoint = $"/preapproval_plan/{externalPlanId}";
                // --- CORREÇÃO: Chama o método da classe base ---
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
            catch (ResourceNotFoundException) // Deixa a exceção específica passar
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

        public async Task DeletePlanAsync(string externalPlanId)
        {
            try
            {
                // No Mercado Pago, planos não são deletados, apenas desativados (status: cancelled)
                var endpoint = $"/preapproval_plan/{externalPlanId}";
                var payload = new { status = "cancelled" };
                // --- CORREÇÃO: Chama o método da classe base ---
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
