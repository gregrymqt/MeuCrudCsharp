using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Resource.Payment;
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

        public async Task<SubscriptionResponseDto> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal users
        )
        {
            var userIdString = users.FindFirstValue(ClaimTypes.NameIdentifier);
            // Nota: É mais seguro buscar o usuário com AsNoTracking() apenas se você não for alterá-lo.
            // Como vamos alterar, removemos o AsNoTracking() para que o EF Core rastreie as mudanças.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdString);

            if (user == null)
            {
                throw new AppServiceException("Usuário não encontrado.");
            }

            string customerId = user.MercadoPagoCustomerId;

            // 2. VERIFICAR e, se necessário, CRIAR o cliente no Mercado Pago
            if (string.IsNullOrEmpty(customerId))
            {
                _logger.LogInformation(
                    "Usuário {UserId} não possui um cliente no MP. Criando agora...",
                    userIdString
                );

                // **CHAMADA CORRETA DO NOVO MÉTODO**
                var newCustomer = await _mercadoPagoService.CreateCustomerAsync(
                    user.Email,
                    user.Name
                );
                customerId = newCustomer.Id;

                // SALVAR o novo ID no seu banco de dados para não criar de novo no futuro!
                user.MercadoPagoCustomerId = customerId;
                // O SaveChanges() será chamado uma única vez no final, junto com a criação da assinatura.
            }

            // **CHAMADA CORRETA DO NOVO MÉTODO**
            // Este passo agora é feito ANTES de criar a assinatura.
            // Nota: O token do cartão ('createDto.CardTokenId') é usado aqui e não mais na criação da assinatura.
            var savedCard = await _mercadoPagoService.SaveCardToCustomerAsync(
                customerId,
                createDto.CardTokenId
            );

            try
            {
                // ATUALIZAÇÃO: Agora passamos o customerId e o savedCard.Id para o método de criação da assinatura.
                // O método CreateSubscriptionAsync precisará ser ajustado para receber esses parâmetros.
                var subscriptionResponse = await _mercadoPagoService.CreateSubscriptionAsync(
                    createDto.PreapprovalPlanId, // ID do plano
                    customerId, // ID do cliente
                    savedCard.Id // ID do cartão salvo
                );

                var localPlan = await _context
                    .Plans.AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId
                    );

                if (localPlan == null)
                {
                    throw new ResourceNotFoundException(
                        $"Plano com ID externo '{subscriptionResponse.PreapprovalPlanId}' não encontrado."
                    );
                }

                if (!Guid.TryParse(userIdString, out var userIdGuid))
                {
                    throw new AppServiceException("ID de usuário inválido na sessão.");
                }

                var newSubscription = new Subscription
                {
                    UserId = userIdGuid,
                    PlanId = localPlan.Id,
                    ExternalId = subscriptionResponse.Id,
                    Status = subscriptionResponse.Status,
                    PayerEmail = subscriptionResponse.PayerEmail,
                    CreatedAt = DateTime.UtcNow,
                    // Adicionar dados do cartão salvo para referência
                    LastFourCardDigits = savedCard.LastFourDigits,
                };

                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync(); // Salva o customerId no usuário E a nova assinatura em uma única transação.

                _logger.LogInformation(
                    "Assinatura {SubscriptionId} criada com sucesso para o usuário {UserId}",
                    newSubscription.ExternalId,
                    userIdString
                );

                return subscriptionResponse;
            }
            catch (HttpRequestException ex) // Captura falhas da API
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao criar assinatura para o usuário {UserId}",
                    userIdString
                );
                throw new ExternalApiException(
                    "Erro ao comunicar com o provedor de pagamento.",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar assinatura para o usuário {UserId}",
                    userIdString
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
