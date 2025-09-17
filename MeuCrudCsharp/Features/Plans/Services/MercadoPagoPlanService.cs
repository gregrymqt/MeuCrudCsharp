using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Plans.Services;

public class MercadoPagoPlanService : MercadoPagoServiceBase, IMercadoPagoPlanService
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
    public MercadoPagoPlanService(
        ApiDbContext context,
        ICacheService cacheService,
        IHttpClientFactory httpClient,
        ILogger<IMercadoPagoPlanService> logger
    )
        : base(httpClient, logger)
    {
        _context = context;
        _cacheService = cacheService;
    }
      /// <inheritdoc />
        public async Task<Plan> CreatePlanAsync(CreatePlanDto createDto)
        {
            // 1. Crie a entidade que será salva no SEU banco de dados.
            var newPlan = new Plan
            {
                Name = createDto.Reason,
                Description = createDto.Description,
                TransactionAmount = createDto.AutoRecurring.TransactionAmount,
                CurrencyId = createDto.AutoRecurring.CurrencyId,
                Frequency = createDto.AutoRecurring.Frequency,
                FrequencyType = createDto.AutoRecurring.FrequencyType,
                IsActive = true,
            };

            // ✅ Salve primeiro para gerar um ID local único.
            _context.Plans.Add(newPlan);
            await _context.SaveChangesAsync();

            // 2. Crie um payload específico para o Mercado Pago.
            var mercadoPagoPayload = new
            {
                reason = createDto.Reason,
                auto_recurring = createDto.AutoRecurring,
                back_url = createDto.BackUrl,
                external_reference = newPlan.Id.ToString(), // ✅ Use o ID local como referência externa.
                description = createDto.Description
            };

            try
            {
                // 3. Envie o payload para o Mercado Pago.
                const string endpoint = "/preapproval_plan";
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Post,
                    endpoint,
                    mercadoPagoPayload 
                );
                var mpPlanResponse = JsonSerializer.Deserialize<PlanResponseDto>(responseBody);

                // 4. Atualize sua entidade com o ID externo retornado pelo MP.
                newPlan.ExternalPlanId = mpPlanResponse.Id;
                _context.Plans.Update(newPlan);
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation(
                    "Plano '{PlanName}' criado com sucesso no MP (ID: {MpPlanId}) e salvo localmente (ID: {LocalPlanId}).",
                    newPlan.Name,
                    newPlan.ExternalPlanId,
                    newPlan.Id
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
        public async Task<Plan> UpdatePlanAsync(Guid publicId, UpdatePlanDto updateDto)
        {
            try
            {
                // CORREÇÃO 1: Busca o plano local pelo PublicId para obter o ExternalPlanId
                var localPlan = await GetPlanByPublicIdAsync(publicId);
                var externalPlanId = localPlan.ExternalPlanId;

                var endpoint = $"/preapproval_plan/{externalPlanId}";
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, updateDto);

                // CORREÇÃO 2: Atualiza apenas os campos que foram fornecidos no DTO
                if (updateDto.Reason != null)
                {
                    localPlan.Name = updateDto.Reason;
                }
                if (updateDto.TransactionAmount.HasValue)
                {
                    localPlan.TransactionAmount = updateDto.TransactionAmount.Value;
                }
                if (updateDto.FrequencyType != null)
                {
                    localPlan.FrequencyType = updateDto.FrequencyType;
                }
                // (Adicione outros campos como BackUrl se você os armazena localmente)

                await _context.SaveChangesAsync();

                // CORREÇÃO 3: Invalida ambos os caches onde os planos podem estar
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
                    publicId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao atualizar o plano {PlanId}.",
                    publicId
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
        public async Task DeletePlanAsync(Guid publicId)
        {
            try
            {
                var localPlan = await GetPlanByPublicIdAsync(publicId);
                var externalPlanId = localPlan.ExternalPlanId;

                var endpoint = $"/preapproval_plan/{externalPlanId}";
                var payload = new { status = "cancelled" };
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);

                localPlan.IsActive = false; // Soft delete
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("ActiveSubscriptionPlans");
                _logger.LogInformation("Plano {PlanId} desativado com sucesso.", externalPlanId);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar desativar o plano {PlanId}.",
                    publicId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao desativar o plano {PlanId}.",
                    publicId
                );
                throw new AppServiceException(
                    "Ocorreu um erro em nosso sistema ao desativar o plano.",
                    ex
                );
            }
        }
        public async Task<Plan> GetPlanByPublicIdAsync(Guid publicId)
        {
            var plan = await _context.Plans
                .FirstOrDefaultAsync(p => p.PublicId == publicId);

            if (plan == null)
            {
                throw new ResourceNotFoundException($"Plano com PublicId {publicId} não encontrado.");
            }
            return plan;
        }
}