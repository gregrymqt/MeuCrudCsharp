using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Subscriptions.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICacheService _cacheService;
        private readonly ApiDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IMercadoPagoService mercadoPagoService,
            ICacheService cacheService,
            ApiDbContext context,
            ILogger<SubscriptionService> logger
        )
        {
            _mercadoPagoService = mercadoPagoService;
            _cacheService = cacheService;
            _context = context;
            _logger = logger;
        }

        public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal user
        )
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new AppServiceException("ID de usuário inválido na sessão.");
            }

            try
            {
                var subscriptionResponse = await _mercadoPagoService.CreateSubscriptionAsync(
                    createDto
                );

                var localPlan = await _context
                    .Plans.AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId
                    );

                if (localPlan == null)
                {
                    // Este é um erro crítico de dados. Lançamos uma exceção específica.
                    throw new ResourceNotFoundException(
                        $"Plano com ID externo '{subscriptionResponse.PreapprovalPlanId}' não encontrado no banco de dados."
                    );
                }

                var newSubscription = new Subscription
                {
                    UserId = userId,
                    PlanId = localPlan.Id,
                    ExternalId = subscriptionResponse.Id,
                    Status = subscriptionResponse.Status,
                    PayerEmail = subscriptionResponse.PayerEmail,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Assinatura {SubscriptionId} criada com sucesso para o usuário {UserId}",
                    newSubscription.ExternalId,
                    userId
                );

                return subscriptionResponse;
            }
            catch (ExternalApiException ex) // Captura falhas vindas do MercadoPagoService
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao criar assinatura para o e-mail {PayerEmail}",
                    createDto.PayerEmail
                );
                throw; // Relança a exceção para o controller tratar
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar assinatura para o e-mail {PayerEmail}",
                    createDto.PayerEmail
                );
                throw new AppServiceException("Ocorreu um erro ao processar sua assinatura.", ex);
            }
        }

        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            try
            {
                return await _mercadoPagoService.GetSubscriptionAsync(subscriptionId);
            }
            catch (ExternalApiException) // Deixa a exceção específica passar
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao buscar assinatura com ID {SubscriptionId}",
                    subscriptionId
                );
                throw new AppServiceException("Falha ao buscar dados da assinatura.", ex);
            }
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            try
            {
                var result = await _mercadoPagoService.UpdateSubscriptionValueAsync(
                    subscriptionId,
                    dto
                );

                // Atualiza o valor no plano local para consistência
                var localSubscription = await _context
                    .Subscriptions.Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.ExternalId == subscriptionId);

                if (localSubscription?.Plan != null)
                {
                    localSubscription.Plan.TransactionAmount = dto.TransactionAmount;
                    await _context.SaveChangesAsync();
                    await _cacheService.RemoveAsync(
                        $"SubscriptionDetails_{localSubscription.UserId}"
                    );
                }

                _logger.LogInformation(
                    "Valor da assinatura {SubscriptionId} atualizado.",
                    subscriptionId
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao atualizar o valor da assinatura {SubscriptionId}",
                    subscriptionId
                );
                throw new AppServiceException("Falha ao atualizar o valor da assinatura.", ex);
            }
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        )
        {
            try
            {
                var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
                    subscriptionId,
                    dto.Status
                );

                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.ExternalId == subscriptionId
                );
                if (subscription != null)
                {
                    subscription.Status = dto.Status; // Atualiza o status local
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Invalida o cache do usuário afetado
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{subscription.UserId}");
                }

                _logger.LogInformation(
                    "Status da assinatura {SubscriptionId} atualizado para {Status}.",
                    subscriptionId,
                    dto.Status
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao atualizar o status da assinatura {SubscriptionId}",
                    subscriptionId
                );
                throw new AppServiceException("Falha ao atualizar o status da assinatura.", ex);
            }
        }
    }
}
