using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // MUDANÇA 1: Adicionando o Logger

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly ApiDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UserAccountService> _logger; // MUDANÇA 1

        public UserAccountService(
            ApiDbContext context,
            IMercadoPagoService mercadoPagoService,
            ICacheService cacheService,
            ILogger<UserAccountService> logger
        ) // MUDANÇA 1
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
        {
            string cacheKey = $"UserProfile_{userId}";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var user = await _context
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId.ToString());
                        if (user == null)
                            throw new ResourceNotFoundException(
                                $"Usuário com ID {userId} não encontrado."
                            );

                        return new UserProfileDto
                        {
                            Name = user.Name,
                            Email = user.Email,
                            AvatarUrl = user.AvatarUrl,
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Erro ao buscar perfil do usuário {UserId} no banco de dados.",
                            userId
                        );
                        throw new AppServiceException(
                            "Ocorreu um erro ao buscar os dados do perfil.",
                            ex
                        );
                    }
                },
                TimeSpan.FromMinutes(15)
            );
        }

        public async Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(Guid userId)
        {
            string cacheKey = $"SubscriptionDetails_{userId}";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var subscription = await _context
                            .Subscriptions.AsNoTracking()
                            .Include(s => s.Plan)
                            .FirstOrDefaultAsync(s =>
                                s.UserId == userId && s.Status != "cancelado"
                            );

                        if (subscription?.Plan == null)
                            return null;

                        var mpSubscription = await _mercadoPagoService.GetSubscriptionAsync(
                            subscription.ExternalId
                        );

                        return new SubscriptionDetailsDto
                        {
                            SubscriptionId = subscription.ExternalId,
                            PlanName = subscription.Plan.Name,
                            Status = mpSubscription.Status,
                            Amount = subscription.Plan.TransactionAmount,
                            NextBillingDate = mpSubscription.NextBillingDate,
                            LastFourCardDigits = mpSubscription.Card?.LastFourDigits,
                        };
                    }
                    // Captura exceções específicas da comunicação com a API externa
                    catch (ExternalApiException ex)
                    {
                        _logger.LogError(
                            ex,
                            "Falha ao buscar detalhes da assinatura {SubscriptionId} no Mercado Pago para o usuário {UserId}.",
                            "ID_EXTERNO_AQUI",
                            userId
                        );
                        throw; // Relança para o controller tratar
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Erro ao buscar detalhes da assinatura para o usuário {UserId}.",
                            userId
                        );
                        throw new AppServiceException(
                            "Ocorreu um erro ao buscar os detalhes da sua assinatura.",
                            ex
                        );
                    }
                },
                TimeSpan.FromMinutes(5)
            );
        }

        public async Task<IEnumerable<Models.Payments>> GetUserPaymentHistoryAsync(Guid userId)
        {
            try
            {
                // Opcional: Cache para o histórico de pagamentos
                string? cacheKey = $"PaymentHistory_{userId}";
                return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        return await _context
                            .Payments.AsNoTracking()
                            .Where(p => p.UserId == userId)
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    },
                    TimeSpan.FromMinutes(10)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar histórico de pagamentos para o usuário {UserId}.",
                    userId
                );
                throw new ResourceNotFoundException(
                    "Ocorreu um erro ao buscar seu histórico de pagamentos.",
                    ex
                );
            }
        }

        public async Task<bool> UpdateSubscriptionCardAsync(Guid userId, string newCardToken)
        {
            try
            {
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.UserId == userId && s.Status != "cancelado"
                );
                if (subscription == null)
                    throw new ResourceNotFoundException(
                        "Assinatura ativa não encontrada para atualização."
                    );

                await _mercadoPagoService.UpdateSubscriptionCardAsync(
                    subscription.ExternalId,
                    newCardToken
                );
                await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

                _logger.LogInformation(
                    "Cartão da assinatura {SubscriptionId} do usuário {UserId} foi atualizado.",
                    subscription.ExternalId,
                    userId
                );
                return true;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro da API externa ao tentar atualizar o cartão para o usuário {UserId}.",
                    userId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao atualizar o cartão para o usuário {UserId}.",
                    userId
                );
                throw new AppServiceException("Ocorreu um erro ao atualizar seu cartão.", ex);
            }
        }

        public async Task<bool> CancelSubscriptionAsync(Guid userId)
        {
            try
            {
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.UserId == userId && s.Status != "cancelado"
                );
                if (subscription == null)
                    throw new ResourceNotFoundException(
                        "Assinatura ativa não encontrada para cancelamento."
                    );

                var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
                    subscription.ExternalId,
                    "cancelled"
                );

                if (result.Status == "cancelled")
                {
                    subscription.Status = "cancelado";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
                    _logger.LogInformation(
                        "Assinatura {SubscriptionId} do usuário {UserId} foi cancelada.",
                        subscription.ExternalId,
                        userId
                    );
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao cancelar a assinatura para o usuário {UserId}.",
                    userId
                );
                throw new AppServiceException("Ocorreu um erro ao cancelar sua assinatura.", ex);
            }
        }

        public async Task<bool> ReactivateSubscriptionAsync(Guid userId)
        {
            try
            {
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.UserId == userId && s.Status == "pausado"
                );
                if (subscription == null)
                    return false;

                var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
                    subscription.ExternalId,
                    "authorized"
                );

                if (result.Status == "authorized")
                {
                    subscription.Status = "ativo";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 3. AÇÃO DE ESCRITA: Invalida o cache
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao reativar a assinatura para o usuário {UserId}.",
                    userId
                );
                throw new AppServiceException("Ocorreu um erro ao reativar sua assinatura.", ex);
            }
        }

        // Dentro da classe UserAccountService

        public async Task<Payments> GetPaymentForReceiptAsync(Guid userId, Guid paymentId)
        {
            try
            {
                _logger.LogInformation(
                    "Buscando pagamento {PaymentId} para o usuário {UserId}.",
                    paymentId,
                    userId
                );

                var payment = await _context
                    .Payments.AsNoTracking()
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

                // Lança uma exceção se o pagamento não for encontrado ou não pertencer ao usuário
                if (payment == null)
                {
                    throw new ResourceNotFoundException(
                        $"Pagamento com ID {paymentId} não encontrado ou não pertence ao usuário {userId}."
                    );
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar o pagamento {PaymentId} no banco de dados.",
                    paymentId
                );
                throw new AppServiceException(
                    "Ocorreu um erro ao buscar os dados do seu pagamento.",
                    ex
                );
            }
        }
    }
}
