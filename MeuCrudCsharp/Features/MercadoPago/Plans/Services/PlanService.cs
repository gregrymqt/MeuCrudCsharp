using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Plans.Mappers;
using MeuCrudCsharp.Features.MercadoPago.Plans.Utils;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Services
{
    /// <summary>
    /// Implements <see cref="IPlanService"/> to manage the lifecycle of subscription plans.
    /// This service coordinates operations between the local database and the Mercado Pago API.
    /// </summary>
    public class PlanService : IPlanService
    {
        private static class CacheKeys
        {
            public static readonly string ActiveDbPlans = "ActiveDbPlans";
            public static readonly string ActiveApiPlans = "ActiveApiPlans";
        }

        private readonly ICacheService _cacheService;
        private readonly IPlanRepository _planRepository;
        private readonly IMercadoPagoPlanService _mercadoPagoPlanService;
        private readonly GeneralSettings _generalSettings;
        private readonly ILogger<PlanService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
        public PlanService(
            ICacheService cacheService,
            ILogger<PlanService> logger,
            IPlanRepository planRepository,
            IMercadoPagoPlanService mercadoPagoPlanService,
            IOptions<GeneralSettings> generalSettings
        )
        {
            _cacheService = cacheService;
            _planRepository = planRepository;
            _mercadoPagoPlanService = mercadoPagoPlanService;
            _generalSettings = generalSettings.Value;
            _logger = logger;
        }

        public async Task<PlanEditDto> GetPlanEditDtoByIdAsync(Guid publicId)
        {
            // Este método interno continua buscando a entidade do banco
            var plan = await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: true);

            if (plan == null)
            {
                return null; // Não encontrado
            }

            // Usa o novo método de mapeamento para retornar o DTO de edição
            return PlanMapper.MapPlanToEditDto(plan);
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
                    back_url = _generalSettings.BaseUrl,
                    external_reference = newPlan.PublicId.ToString(),
                };

                var mpPlanResponse = await _mercadoPagoPlanService.CreatePlanAsync(mercadoPagoPayload);

                // 5. Atualiza a entidade local com o ID externo
                newPlan.ExternalPlanId = mpPlanResponse.Id;
                _planRepository.Update(newPlan);
                await _planRepository.SaveChangesAsync();

                // 6. Pós-processamento (cache, log)
                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                _logger.LogInformation("Plano '{PlanName}' criado com sucesso.", newPlan.Name);

                return newPlan;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Erro na API externa ao criar plano '{PlanName}'. Iniciando rollback...",
                    createDto.Reason);

                // ✅ AÇÃO DE COMPENSAÇÃO (ROLLBACK)
                // A chamada para a API falhou, então removemos o plano "órfão" que criamos localmente.
                _planRepository.Remove(newPlan);
                await _planRepository.SaveChangesAsync();
                _logger.LogInformation("Rollback concluído. Plano local '{PlanName}' foi removido do banco de dados.",
                    createDto.Reason);

                // Relança a exceção para que o controller possa recebê-la e informar o erro ao usuário.
                throw;
            }
        }

        public async Task<Plan> UpdatePlanAsync(Guid publicId, UpdatePlanDto updateDto)
        {
            // 1. Busca a entidade local (agora rastreada pelo EF Core)
            var localPlan = await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: false)
                            ?? throw new ResourceNotFoundException($"Plano com ID {publicId} não encontrado.");

            // 2. Guarda os valores originais para possível rollback
            var originalValues = new
            {
                Name = localPlan.Name,
                TransactionAmount = localPlan.TransactionAmount,
                FrequencyInterval = localPlan.FrequencyInterval,
                FrequencyType = localPlan.FrequencyType
            };

            PlanUtils.ApplyUpdateDtoToPlan(localPlan, updateDto);
            await _planRepository.SaveChangesAsync();
            _logger.LogInformation("Alterações locais para o plano {PlanId} salvas temporariamente.", localPlan.Id);

            try
            {
                var payloadForMercadoPago = new
                {
                    reason = updateDto.Reason,
                    auto_recurring = updateDto.AutoRecurring
                };
                await _mercadoPagoPlanService.UpdatePlanAsync(localPlan.ExternalPlanId, payloadForMercadoPago);

                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                _logger.LogInformation("Plano {PlanId} atualizado com sucesso no DB e na API externa.",
                    localPlan.ExternalPlanId);

                return localPlan;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Erro na API externa ao atualizar plano '{PlanName}'. Iniciando rollback...",
                    localPlan.Name);

                localPlan.Name = originalValues.Name;
                localPlan.TransactionAmount = originalValues.TransactionAmount;
                localPlan.FrequencyInterval = originalValues.FrequencyInterval;
                localPlan.FrequencyType = originalValues.FrequencyType;

                await _planRepository.SaveChangesAsync();
                _logger.LogInformation("Rollback concluído. Alterações locais no plano '{PlanName}' foram desfeitas.",
                    localPlan.Name);

                throw;
            }
        }


        public async Task DeletePlanAsync(Guid publicId)
        {
            var localPlan = await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: false)
                            ?? throw new ResourceNotFoundException($"Plano com ID {publicId} não encontrado.");

            var originalIsActive = localPlan.IsActive;

            if (!originalIsActive)
            {
                _logger.LogWarning("Tentativa de desativar o plano {PlanId} que já está inativo.",
                    localPlan.ExternalPlanId);
                return;
            }

            localPlan.IsActive = false;
            await _planRepository.SaveChangesAsync();
            _logger.LogInformation("Plano {PlanId} desativado localmente de forma temporária.",
                localPlan.ExternalPlanId);

            try
            {
                await _mercadoPagoPlanService.CancelPlanAsync(localPlan.ExternalPlanId);

                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                _logger.LogInformation("Plano {PlanId} desativado com sucesso no DB e na API externa.",
                    localPlan.ExternalPlanId);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Erro na API externa ao desativar plano '{PlanName}'. Iniciando rollback...",
                    localPlan.Name);

                localPlan.IsActive = originalIsActive;
                await _planRepository.SaveChangesAsync();
                _logger.LogInformation("Rollback concluído. Plano '{PlanName}' foi reativado localmente.",
                    localPlan.Name);

                throw;
            }
        }

        public async Task<PagedResultDto<PlanDto>> GetActiveApiPlansAsync(int page, int pageSize)
        {
            // 1. Busca a LISTA COMPLETA do cache (ou da API se o cache estiver vazio)
            var allCachedPlans = await _cacheService.GetOrCreateAsync(
                CacheKeys.ActiveApiPlans,
                async () =>
                {
                    _logger.LogInformation("Cache de planos da API vazio. Buscando do Mercado Pago.");
                    // A lógica interna para buscar todos os planos da API e mapeá-los permanece a mesma.
                    var activePlansFromApi = await _mercadoPagoPlanService.SearchActivePlansAsync();
                    if (!activePlansFromApi.Any())
                    {
                        return new List<PlanDto>();
                    }

                    var externalIds = activePlansFromApi.Select(p => p.Id).ToList();
                    var localPlans = await _planRepository.GetByExternalIdsAsync(externalIds);
                    var localPlansDict = localPlans.ToDictionary(p => p.ExternalPlanId, p => p);

                    var mappedPlans = new List<PlanDto>();
                    foreach (var apiPlan in activePlansFromApi)
                    {
                        if (localPlansDict.TryGetValue(apiPlan.Id, out var localPlan))
                        {
                            mappedPlans.Add(PlanMapper.MapApiPlanToDto(apiPlan, localPlan));
                        }
                        else
                        {
                            _logger.LogWarning("Plano '{ExternalId}' existe no MP mas não localmente.", apiPlan.Id);
                        }
                    }

                    return mappedPlans;
                },
                TimeSpan.FromMinutes(5) // Tempo de expiração do cache
            );

            var fullPlanList = allCachedPlans ?? new List<PlanDto>();

            // 2. Aplica a paginação na lista completa que veio do cache
            var totalCount = fullPlanList.Count;
            var itemsForPage = fullPlanList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 3. Retorna o objeto de resultado paginado
            return new PagedResultDto<PlanDto>(
                itemsForPage,
                page,
                pageSize,
                totalCount
            );
        }


        /// <summary>
        /// Busca os planos ativos diretamente do banco de dados local.
        /// </summary>
        public async Task<PagedResultDto<PlanDto>> GetActiveDbPlansAsync(int page, int pageSize)
        {
            _logger.LogInformation("Buscando planos ativos paginados do banco de dados.");

            try
            {
                // 1. Chama o repositório com os parâmetros de paginação
                var pagedResult = await _planRepository.GetActivePlansAsync(page, pageSize);

                // 2. Mapeia apenas os itens da página atual para DTOs
                var planDtos = pagedResult.Items
                    .Select(PlanMapper.MapDbPlanToDto)
                    .ToList();

                // 3. Cria um novo PagedResultDto com os itens mapeados e os metadados
                return new PagedResultDto<PlanDto>(
                    planDtos,
                    pagedResult.CurrentPage,
                    pagedResult.PageSize,
                    pagedResult.TotalCount
                );
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Falha ao buscar planos paginados do repositório.");
                throw new AppServiceException("Não foi possível carregar os planos.", dbEx);
            }
        }
    }
}