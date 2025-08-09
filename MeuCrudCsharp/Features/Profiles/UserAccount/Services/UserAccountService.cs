// Local: Features/Profiles/UserAccount/Services/UserAccountService.cs

using System.Security.Claims;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base; // Importando a classe base
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs; // Importando DTOs de assinatura
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
    // --- CORREÇÃO: Herda da classe base ---
    public class UserAccountService : MercadoPagoServiceBase, IUserAccountService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService; // Descomente quando o serviço de cache estiver implementado

        // --- CORREÇÃO: Construtor ajustado para injetar dependências corretas ---
        public UserAccountService(
            ApiDbContext context,
            ICacheService cacheService,
            HttpClient httpClient,
            ILogger<UserAccountService> logger
        )
            : base(httpClient, logger) // Passa dependências para a classe base
        {
            _context = context;
            _cacheService = cacheService;
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

                        // --- CORREÇÃO: Lógica da API movida para dentro do método ---
                        var endpoint = $"/preapproval/{subscription.ExternalId}";
                        var responseBody = await SendMercadoPagoRequestAsync(
                            HttpMethod.Get,
                            endpoint,
                            (object?)null
                        );
                        var mpSubscription = JsonSerializer.Deserialize<SubscriptionResponseDto>(
                            responseBody
                        );

                        if (mpSubscription == null)
                            return null;

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
                }
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
                var subscription = await FindActiveSubscriptionAsync(
                    userId,
                    "para atualização de cartão"
                );

                // --- CORREÇÃO: Lógica da API movida para dentro do método ---
                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new UpdateCardTokenDto { NewCardToken = newCardToken };
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);

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
                var subscription = await FindActiveSubscriptionAsync(userId, "para cancelamento");

                // --- CORREÇÃO: Lógica da API movida para dentro do método ---
                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new SubscriptionDetailsDto { Status = "cancelled" };
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Put,
                    endpoint,
                    payload
                );
                var result = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

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
                    return false; // Não é um erro, apenas não há o que reativar

                // --- CORREÇÃO: Lógica da API movida para dentro do método ---
                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new SubscriptionDetailsDto { Status = "authorized" };
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Put,
                    endpoint,
                    payload
                );
                var result = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

                if (result.Status == "authorized")
                {
                    subscription.Status = "ativo";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
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

        private async Task<Subscription> FindActiveSubscriptionAsync(Guid userId, string action)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && (s.Status == "ativo" || s.Status == "pausado")
            );

            if (subscription == null)
                throw new ResourceNotFoundException($"Assinatura ativa não encontrada {action}.");

            return subscription;
        }
    }
}
