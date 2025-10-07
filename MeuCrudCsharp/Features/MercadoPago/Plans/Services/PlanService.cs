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
        private readonly ICacheService _cacheService;
        private readonly IPlanRepository _planRepository;
        private readonly IMercadoPagoPlanService _mercadoPagoPlanService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
        public PlanService(
            ICacheService cacheService,
            IHttpClientFactory httpClient,
            ILogger<PlanService> logger,
            IPlanRepository planRepository,
            IMercadoPagoPlanService mercadoPagoPlanService
        )
            : base(httpClient, logger)
        {
            _cacheService = cacheService;
            _planRepository = planRepository;
            _mercadoPagoPlanService = mercadoPagoPlanService;
        }

        public async Task<Plan> GetPlanByIdAsync(Guid publicId)
        {
            if (publicId == Guid.Empty)
            {
                throw new ArgumentException("PublicId cannot be empty");
            }
            return await _planRepository.GetByPublicIdAsync(publicId);
            
        }

        public async Task<Plan> CreatePlanAsync(CreatePlanDto createDto)
        {
            // 1. Validação de Lógica de Negócio
            if (!Enum.TryParse<PlanFrequencyType>(createDto.AutoRecurring.FrequencyType, true,
                    out var frequencyTypeEnum))
            {
                throw new ArgumentException(
                    $"Valor de frequência inválido: '{createDto.AutoRecurring.FrequencyType}'.");
            }

            // 2. Criação da Entidade Local
            var newPlan = new Plan
            {
                Name = createDto.Reason,
                Description = createDto.Description,
                TransactionAmount = createDto.AutoRecurring.TransactionAmount,
                CurrencyId = createDto.AutoRecurring.CurrencyId,
                FrequencyInterval = createDto.AutoRecurring.Frequency,
                FrequencyType = frequencyTypeEnum,
                IsActive = true,
            };

            // 3. Persistência inicial no banco de dados (para gerar o ID local)
            await _planRepository.AddAsync(newPlan);
            await _planRepository.SaveChangesAsync();

            // 4. Criação do plano na API externa
            try
            {
                var mercadoPagoPayload = new
                {
                    reason = createDto.Reason,
                    auto_recurring = createDto.AutoRecurring,
                    back_url = createDto.BackUrl,
                    external_reference = newPlan.Id.ToString(), // ✅ Use o ID local como referência externa.
                    description = createDto.Description
                };

                var mpPlanResponse = await _mercadoPagoPlanService.CreatePlanAsync(mercadoPagoPayload);

                // 5. Atualiza a entidade local com o ID externo
                newPlan.ExternalPlanId = mpPlanResponse.Id;
                _planRepository.Update(newPlan);
                await _planRepository.SaveChangesAsync();

                // 6. Pós-processamento (cache, log)
                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation("Plano '{PlanName}' criado com sucesso.", newPlan.Name);

                return newPlan;
            }
            catch (ExternalApiException ex)
            {
                // Se a API externa falhar, idealmente você deveria tratar o "plano órfão"
                // que foi criado localmente. Poderia deletá-lo ou marcá-lo como "falhou".
                _logger.LogError(ex, "Erro da API ao criar plano '{PlanName}'.", createDto.Reason);
                throw;
            }
        }

        public async Task<Plan> UpdatePlanAsync(Guid publicId, UpdatePlanDto updateDto)
        {
            // 1. Busca a entidade local
            var localPlan = await _planRepository.GetByPublicIdAsync(publicId);

            if (updateDto.Reason != null) localPlan.Name = updateDto.Reason;
            if (updateDto.TransactionAmount.HasValue) localPlan.TransactionAmount = updateDto.TransactionAmount.Value;
            if (updateDto.Frequency.HasValue)
                localPlan.FrequencyInterval = updateDto.Frequency.Value;
            if (updateDto.FrequencyType != null)
            {
                if (!Enum.TryParse<PlanFrequencyType>(updateDto.FrequencyType, ignoreCase: true,
                        out var frequencyTypeEnum))
                {
                    throw new ArgumentException(
                        $"O valor '{updateDto.FrequencyType}' é inválido para o tipo de frequência. Use 'Days' ou 'Months'.");
                }

                localPlan.FrequencyType = frequencyTypeEnum;
            }

            var payloadForMercadoPago = new
            {
                reason = localPlan.Name,
                transaction_amount = localPlan.TransactionAmount,
                frequency = localPlan.FrequencyInterval, // Usa a propriedade correta
                frequency_type =
                    localPlan.FrequencyType.ToString().ToLower() // Converte o enum para string minúscula (ex: "months")
            };
            await _mercadoPagoPlanService.UpdatePlanAsync(localPlan.ExternalPlanId, payloadForMercadoPago);

            // 4. Persiste as alterações localmente (somente se a API não falhou)
            _planRepository.Update(localPlan);
            await _planRepository.SaveChangesAsync();

            // 5. Limpa o cache
            await _cacheService.RemoveAsync("DbPlansCacheKey");
            _logger.LogInformation("Plano {PlanId} atualizado com sucesso.", localPlan.ExternalPlanId);

            return localPlan;
        }

        public async Task DeletePlanAsync(Guid publicId)
        {
            var localPlan = await _planRepository.GetByPublicIdAsync(publicId);

            // 1. Desativa na API externa primeiro
            await _mercadoPagoPlanService.CancelPlanAsync(localPlan.ExternalPlanId);

            // 2. Desativa ("soft delete") localmente
            localPlan.IsActive = false;
            _planRepository.Update(localPlan);
            await _planRepository.SaveChangesAsync();

            await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
            _logger.LogInformation("Plano {PlanId} desativado com sucesso.", localPlan.ExternalPlanId);
        }

        public async Task<List<PlanDto>> GetActiveApiPlansAsync()
        {
            // O cache continua sendo responsabilidade do serviço de orquestração
            var cachedPlans = await _cacheService.GetOrCreateAsync(
                "ApiPlansCacheKey", // Corrigido
                async () =>
                {
                    // 1. Busca todos os planos ativos da API
                    var activePlansFromApi = await _mercadoPagoPlanService.SearchActivePlansAsync();
                    if (!activePlansFromApi.Any())
                    {
                        return new List<PlanDto>();
                    }

                    var externalIds = activePlansFromApi.Select(p => p.Id).ToList();
                    var localPlans = await _planRepository.GetByExternalIdsAsync(externalIds);
                    var localPlansDict = localPlans.ToDictionary(p => p.ExternalPlanId, p => p);

                    // 3. Mapeia os DTOs em memória
                    var mappedPlans = new List<PlanDto>();
                    foreach (var apiPlan in activePlansFromApi)
                    {
                        if (localPlansDict.TryGetValue(apiPlan.Id, out var localPlan))
                        {
                            mappedPlans.Add(MapApiPlanToDto(apiPlan, localPlan));
                        }
                        else
                        {
                            _logger.LogWarning("Plano '{ExternalId}' existe no MP mas não localmente.", apiPlan.Id);
                        }
                    }

                    return mappedPlans;
                },
                TimeSpan.FromMinutes(5)
            );

            return cachedPlans ?? new List<PlanDto>();
        }


        /// <summary>
        /// Busca os planos ativos diretamente do banco de dados local.
        /// </summary>
        public async Task<List<PlanDto>> GetActiveDbPlansAsync()
        {
            _logger.LogInformation("Buscando planos ativos do banco de dados (com cache).");

            var cachedPlans = await _cacheService.GetOrCreateAsync(
                "DbPlansCacheKey", // Corrigido
                FetchAndMapPlansFromDatabaseAsync, // A função de fábrica agora chama o repositório
                TimeSpan.FromMinutes(15)
            );

            return cachedPlans ?? new List<PlanDto>();
        }

        private async Task<List<PlanDto>> FetchAndMapPlansFromDatabaseAsync()
        {
            try
            {
                // 1. Acessa o banco de dados através do repositório
                var plansFromDb = await _planRepository.GetActivePlansAsync();

                // 2. Mapeia as entidades para o DTO (esta lógica permanece no serviço)
                return plansFromDb.Select(MapDbPlanToDto).ToList();
            }
            catch (Exception dbEx) // A captura de exceção de DB faz mais sentido aqui
            {
                _logger.LogError(dbEx, "Falha ao buscar planos do repositório.");
                throw new AppServiceException("Não foi possível carregar os planos.", dbEx);
            }
        }

// --- MÉTODOS AUXILIARES ---

        private PlanDto MapDbPlanToDto(Plan dbPlan)
        {
            string planType = GetPlanTypeDescription(dbPlan.FrequencyInterval, dbPlan.FrequencyType);

            bool isAnnual = dbPlan.FrequencyInterval == 12 && dbPlan.FrequencyType == PlanFrequencyType.Months;

            return new PlanDto
            (
                dbPlan.PublicId.ToString(),
                dbPlan.Name,
                planType, // <-- Usa a descrição flexível (ex: "Mensal", "Trimestral", "Anual")
                FormatPriceDisplay(dbPlan.TransactionAmount, dbPlan.FrequencyInterval), // <-- Usa a propriedade correta
                FormatBillingInfo(dbPlan.TransactionAmount, dbPlan.FrequencyInterval), // <-- Usa a propriedade correta
                GetDefaultFeatures(),
                isAnnual
            );
        }

        private  PlanDto MapApiPlanToDto(PlanResponseDto apiPlan,
            Plan localPlan)
        {
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

        /// <summary>
        /// Função auxiliar para criar uma descrição amigável do tipo de plano.
        /// </summary>
        private string GetPlanTypeDescription(int interval, PlanFrequencyType frequencyType)
        {
            if (frequencyType == PlanFrequencyType.Months)
            {
                switch (interval)
                {
                    case 1:
                        return "Mensal";
                    case 3:
                        return "Trimestral";
                    case 6:
                        return "Semestral";
                    case 12:
                        return "Anual";
                    default:
                        // Fallback para outros intervalos de meses (ex: 2, 4 meses)
                        return $"Pacote de {interval} meses";
                }
            }

            if (frequencyType == PlanFrequencyType.Days)
            {
                // Lógica para planos baseados em dias, se você tiver
                return interval == 1 ? "Diário" : $"Pacote de {interval} dias";
            }

            return "Plano Padrão"; // Fallback genérico
        }

// Método para evitar repetição da lista de features
        public List<string> GetDefaultFeatures() => new List<string>
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
        public string FormatPriceDisplay(decimal amount, int frequency)
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
        public string FormatBillingInfo(decimal amount, int frequency)
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