using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly ApiDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICacheService _cacheService; // 1. Injetamos o serviço de cache

        public UserAccountService(
            ApiDbContext context,
            IMercadoPagoService mercadoPagoService,
            ICacheService cacheService
        )
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _cacheService = cacheService; // 2. Inicializamos no construtor
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
        {
            // Cache para o perfil do usuário
            string? cacheKey = $"UserProfile_{userId}";
            return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        var user = await _context
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId.ToString());

                        if (user == null)
                            throw new KeyNotFoundException("Usuário não encontrado.");

                        return new UserProfileDto
                        {
                            Name = user.Name,
                            Email = user.Email,
                            AvatarUrl = user.AvatarUrl,
                        };
                    },
                    TimeSpan.FromMinutes(15)
                ) ?? throw new InvalidOperationException("Erro ao obter perfil do usuário."); // Cache de 15 minutos
        }

        public async Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(Guid userId)
        {
            // Cache para os detalhes da assinatura
            string cacheKey = $"SubscriptionDetails_{userId}";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    var subscription = await _context
                        .Subscriptions.AsNoTracking()
                        .Include(s => s.Plan)
                        .FirstOrDefaultAsync(s => s.UserId == userId && s.Status != "cancelado");

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
                },
                TimeSpan.FromMinutes(5)
            ); // Cache de 5 minutos
        }

        public async Task<IEnumerable<Models.Payments>> GetUserPaymentHistoryAsync(Guid userId)
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
                ) ?? throw new InvalidOperationException("Erro ao obter histórico de pagamentos do usuário.");
        }

        public async Task<bool> UpdateSubscriptionCardAsync(Guid userId, string newCardToken)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.Status != "cancelado"
            );
            if (subscription == null)
                return false;

            await _mercadoPagoService.UpdateSubscriptionCardAsync(
                subscription.ExternalId,
                newCardToken
            );

            // 3. AÇÃO DE ESCRITA: Invalida o cache para forçar a busca de dados atualizados
            await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

            return true;
        }

        public async Task<bool> CancelSubscriptionAsync(Guid userId)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.Status != "cancelado"
            );
            if (subscription == null)
                return false;

            var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
                subscription.ExternalId,
                "cancelled"
            );

            if (result.Status == "cancelled")
            {
                subscription.Status = "cancelado";
                subscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 3. AÇÃO DE ESCRITA: Invalida o cache
                await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

                return true;
            }
            return false;
        }

        public async Task<bool> ReactivateSubscriptionAsync(Guid userId)
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
    }
}
