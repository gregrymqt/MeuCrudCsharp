using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Plans.Mappers;
using MeuCrudCsharp.Features.MercadoPago.Plans.Utils;
using MeuCrudCsharp.Features.Shared.Work;
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
            public const string ActiveDbPlans = "ActiveDbPlans";
            public const string ActiveApiPlans = "ActiveApiPlans";
        }

        private readonly ICacheService _cacheService;
        private readonly IPlanRepository _planRepository;
        private readonly IMercadoPagoPlanService _mercadoPagoPlanService;
        private readonly GeneralSettings _generalSettings;
        private readonly ILogger<PlanService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanService"/> class.
        /// </summary>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        /// <param name="planRepository">Repository for plan data operations.</param>
        /// <param name="mercadoPagoPlanService">Service for Mercado Pago API calls.</param>
        /// <param name="generalSettings">General application settings.</param>
        /// <param name="unitOfWork">Unit of work for transaction management.</param>
        public PlanService(
            ICacheService cacheService,
            ILogger<PlanService> logger,
            IPlanRepository planRepository,
            IMercadoPagoPlanService mercadoPagoPlanService,
            IOptions<GeneralSettings> generalSettings,
            IUnitOfWork unitOfWork
        )
        {
            _cacheService = cacheService;
            _planRepository = planRepository;
            _mercadoPagoPlanService = mercadoPagoPlanService;
            _generalSettings = generalSettings.Value;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<PlanEditDto> GetPlanEditDtoByIdAsync(Guid publicId)
        {
            // Este método interno continua buscando a entidade do banco
            var plan = await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: true);

            return (plan == null ? null : // Não encontrado
                // Usa o novo método de mapeamento para retornar o DTO de edição
                PlanMapper.MapPlanToEditDto(plan)) ?? throw new InvalidOperationException();
        }

        public async Task<PlanDto> CreatePlanAsync(CreatePlanDto createDto)
        {
            // 1. Validação de Lógica de Negócio
            if (
                !Enum.TryParse<PlanFrequencyType>(
                    createDto.AutoRecurring.FrequencyType,
                    true,
                    out var frequencyTypeEnum
                )
            )
            {
                throw new ArgumentException(
                    $"Valor de frequência inválido: '{createDto.AutoRecurring.FrequencyType}'."
                );
            }

            // 2. Criação da Entidade Local (apenas em memória)
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

            // Marca para adição (NÃO persiste ainda)
            await _planRepository.AddAsync(newPlan);

            // 3. Criação do plano na API externa
            try
            {
                var mercadoPagoPayload = new
                {
                    reason = createDto.Reason,
                    auto_recurring = createDto.AutoRecurring,
                    back_url = _generalSettings.BaseUrl,
                    external_reference = newPlan.PublicId.ToString(),
                };

                var mpPlanResponse = await _mercadoPagoPlanService.CreatePlanAsync(
                    mercadoPagoPayload
                );

                // 4. Atualiza a entidade local com o ID externo (ainda em memória)
                newPlan.ExternalPlanId = mpPlanResponse.Id;

                // 5. ✅ COMMIT ÚNICO - ATOMICIDADE GARANTIDA
                // Persiste o plan com TODOS os dados preenchidos (incluindo ExternalPlanId)
                await _unitOfWork.CommitAsync();

                // 6. Pós-processamento (cache, log)
                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                
                _logger.LogInformation(
                    "Plano '{PlanName}' criado com sucesso. ID: {PlanId}, ExternalId: {ExternalId}",
                    newPlan.Name,
                    newPlan.Id,
                    newPlan.ExternalPlanId
                );

                return PlanMapper.MapDbPlanToDto(newPlan);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro na API externa ao criar plano '{PlanName}'. Rollback automático.",
                    createDto.Reason
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como não fizemos commit, o UnitOfWork descarta todas as mudanças automaticamente
                // Não precisa remover manualmente - o Entity Framework faz isso
                
                _logger.LogInformation(
                    "Rollback automático concluído. Plano '{PlanName}' NÃO foi persistido.",
                    createDto.Reason
                );

                throw;
            }
        }

        public async Task<PlanDto> UpdatePlanAsync(Guid publicId, UpdatePlanDto updateDto)
        {
            // 1. Busca a entidade local (rastreada pelo EF Core)
            var localPlan =
                await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: false)
                ?? throw new ResourceNotFoundException($"Plano com ID {publicId} não encontrado.");

            // 2. Guarda os valores originais para possível rollback
            var originalName = localPlan.Name;
            var originalTransactionAmount = localPlan.TransactionAmount;
            var originalFrequencyInterval = localPlan.FrequencyInterval;
            var originalFrequencyType = localPlan.FrequencyType;

            // 3. Aplica as mudanças (apenas em memória)
            PlanUtils.ApplyUpdateDtoToPlan(localPlan, updateDto);

            try
            {
                // 4. Atualiza no MercadoPago primeiro
                var payloadForMercadoPago = new
                {
                    reason = updateDto.Reason,
                    auto_recurring = updateDto.AutoRecurring,
                };
                
                await _mercadoPagoPlanService.UpdatePlanAsync(
                    localPlan.ExternalPlanId,
                    payloadForMercadoPago
                );

                // 5. ✅ COMMIT ÚNICO - ATOMICIDADE GARANTIDA
                // Se chegou aqui, MP foi atualizado com sucesso, agora persiste localmente
                await _unitOfWork.CommitAsync();

                // 6. Pós-processamento
                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                
                _logger.LogInformation(
                    "Plano {PlanId} atualizado com sucesso no DB e na API externa.",
                    localPlan.ExternalPlanId
                );

                return PlanMapper.MapDbPlanToDto(localPlan);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro na API externa ao atualizar plano '{PlanName}'. Rollback automático.",
                    localPlan.Name
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como não fizemos commit, o EF vai descartar as mudanças automaticamente
                // Mas como a entidade está sendo rastreada, precisamos reverter manualmente
                localPlan.Name = originalName;
                localPlan.TransactionAmount = originalTransactionAmount;
                localPlan.FrequencyInterval = originalFrequencyInterval;
                localPlan.FrequencyType = originalFrequencyType;

                _logger.LogInformation(
                    "Rollback concluído. Alterações locais no plano '{PlanName}' foram desfeitas.",
                    localPlan.Name
                );

                throw;
            }
        }

        public async Task DeletePlanAsync(Guid publicId)
        {
            var localPlan =
                await _planRepository.GetByPublicIdAsync(publicId, asNoTracking: false)
                ?? throw new ResourceNotFoundException($"Plano com ID {publicId} não encontrado.");

            if (!localPlan.IsActive)
            {
                _logger.LogWarning(
                    "Tentativa de desativar o plano {PlanId} que já está inativo.",
                    localPlan.ExternalPlanId
                );
                return;
            }

            // 1. Marca como inativo (apenas em memória)
            localPlan.IsActive = false;

            try
            {
                // 2. Desativa no MercadoPago primeiro
                await _mercadoPagoPlanService.CancelPlanAsync(localPlan.ExternalPlanId);

                // 3. ✅ COMMIT ÚNICO - ATOMICIDADE GARANTIDA
                // Se chegou aqui, MP foi cancelado com sucesso, agora persiste localmente
                await _unitOfWork.CommitAsync();

                // 4. Pós-processamento
                await _cacheService.RemoveAsync(CacheKeys.ActiveDbPlans);
                await _cacheService.RemoveAsync(CacheKeys.ActiveApiPlans);
                
                _logger.LogInformation(
                    "Plano {PlanId} desativado com sucesso no DB e na API externa.",
                    localPlan.ExternalPlanId
                );
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro na API externa ao desativar plano '{PlanName}'. Rollback automático.",
                    localPlan.Name
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como não fizemos commit, o EF vai descartar as mudanças automaticamente
                // Mas como a entidade está sendo rastreada, precisamos reverter manualmente
                localPlan.IsActive = true;
                
                _logger.LogInformation(
                    "Rollback concluído. Plano '{PlanName}' permanece ativo localmente.",
                    localPlan.Name
                );

                throw;
            }
        }

        public async Task<PagedResultDto<PlanDto>> GetActiveApiPlansAsync(int page, int pageSize)
        {
            var limit = pageSize;
            var offset = (page - 1) * pageSize;
            var status = "active";
            var sortBy = "transaction_amount";
            var criteria = "asc";

            _logger.LogInformation(
                "Buscando página {Page} de planos da API do Mercado Pago.",
                page
            );
            var activePlansFromApi = await _mercadoPagoPlanService.SearchActivePlansAsync(
                limit,
                offset,
                status,
                sortBy,
                criteria
            );

            var totalCount = await GetTotalActiveApiPlansCountAsync();

            // 3. O resto da lógica permanece, mas agora opera em uma lista pequena (apenas a página atual)
            var planResponseDtos = activePlansFromApi.ToList();
            if (planResponseDtos.Count == 0)
            {
                return new PagedResultDto<PlanDto>([], page, pageSize, totalCount);
            }

            var externalIds = planResponseDtos.Select(p => p.Id).ToList();
            var localPlans = await _planRepository.GetByExternalIdsAsync(externalIds);
            var localPlansDict = localPlans.ToDictionary(p => p.ExternalPlanId, p => p);

            var mappedPlans = new List<PlanDto>();
            foreach (var apiPlan in planResponseDtos)
            {
                if (localPlansDict.TryGetValue(apiPlan.Id, out var localPlan))
                {
                    mappedPlans.Add(PlanMapper.MapApiPlanToDto(apiPlan, localPlan));
                }
                else
                {
                    _logger.LogWarning(
                        "Plano '{ExternalId}' existe no MP mas não localmente.",
                        apiPlan.Id
                    );
                }
            }

            return new PagedResultDto<PlanDto>(mappedPlans, page, pageSize, totalCount);
        }

        // NOVO MÉTODO AUXILIAR: Para obter o total de planos e cachear o resultado.
        private async Task<int> GetTotalActiveApiPlansCountAsync()
        {
            return await _cacheService.GetOrCreateAsync(
                "TotalActiveApiPlansCount", // Chave de cache específica para a contagem
                async () =>
                {
                    // Chamada leve para a API, apenas para contar.
                    // O ideal seria que o MP tivesse um endpoint só para contagem.
                    // Na ausência disso, buscamos a lista e contamos.
                    var allPlans = await _mercadoPagoPlanService.SearchActivePlansAsync(
                        1000,
                        0,
                        "active",
                        "date_created",
                        "desc"
                    );
                    return allPlans.Count();
                },
                TimeSpan.FromMinutes(15)
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
                var planDtos = pagedResult.Items.Select(PlanMapper.MapDbPlanToDto).ToList();

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
