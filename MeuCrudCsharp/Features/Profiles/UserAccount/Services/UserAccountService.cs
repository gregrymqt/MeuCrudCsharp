using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models; // Adicionado para ter acesso ao modelo Payment
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services;

public class UserAccountService : IUserAccountService
{
    private readonly ApiDbContext _context;
    private readonly IMercadoPagoService _mercadoPagoService; // Serviço que fala com a API do MP

    public UserAccountService(ApiDbContext context, IMercadoPagoService mercadoPagoService)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
    }

    /// <summary>
    /// Busca o perfil de um usuário a partir do seu ID.
    /// </summary>
    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        var user = await _context
            .Users.AsNoTracking() // Melhora a performance para operações de apenas leitura
            .FirstOrDefaultAsync(u => u.Id == userId.ToString());

        if (user == null)
        {
            // Lançar uma exceção é uma boa prática quando o recurso esperado não é encontrado.
            throw new KeyNotFoundException("Usuário não encontrado.");
        }

        return new UserProfileDto
        {
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
        };
    }

    /// <summary>
    /// Busca os detalhes da assinatura ativa de um usuário.
    /// </summary>
    public async Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(Guid userId)
    {
        var subscription = await _context
            .Subscriptions.AsNoTracking()
            .Include(s => s.Plan) // Inclui dados do plano do nosso banco
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status != "cancelado");

        if (subscription?.Plan == null)
        {
            return null;
        }

        // Busca dados adicionais e atualizados da API do Mercado Pago
        // (Assume-se que seu IMercadoPagoService tem um método para buscar uma assinatura)
        var mpSubscription = await _mercadoPagoService.GetSubscriptionAsync(
            subscription.ExternalId
        );

        return new SubscriptionDetailsDto
        {
            SubscriptionId = subscription.ExternalId,
            PlanName = subscription.Plan.Name,
            Status = mpSubscription.Status, // Usa o status mais atual, vindo da API
            Amount = subscription.Plan.TransactionAmount,
            NextBillingDate = mpSubscription.NextBillingDate,
            LastFourCardDigits = mpSubscription.Card?.LastFourDigits, // O '?' evita erro se não houver cartão
        };
    }

    /// <summary>
    /// Retorna o histórico de pagamentos de um usuário.
    /// </summary>
    public async Task<IEnumerable<Models.Payments>> GetUserPaymentHistoryAsync(Guid userId)
    {
        // CORREÇÃO AQUI: de _context.Payment_User para _context.Payments
        return await _context
            .Payments.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Atualiza o cartão de uma assinatura existente.
    /// </summary>
    public async Task<bool> UpdateSubscriptionCardAsync(Guid userId, string newCardToken)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
            s.UserId == userId && s.Status != "cancelado"
        );
        if (subscription == null)
        {
            return false;
        }

        // Chama o serviço de baixo nível para fazer a chamada PUT para a API do MP
        await _mercadoPagoService.UpdateSubscriptionCardAsync(
            subscription.ExternalId,
            newCardToken
        );

        // Opcional: Você pode querer salvar um log ou atualizar um campo localmente
        // subscription.UpdatedAt = DateTime.UtcNow;
        // await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Cancela a assinatura de um usuário no Mercado Pago e no banco local.
    /// </summary>
    public async Task<bool> CancelSubscriptionAsync(Guid userId)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
            s.UserId == userId && s.Status != "cancelado"
        );
        if (subscription == null)
        {
            return false;
        }

        // Chama a API do MP para cancelar a assinatura
        var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
            subscription.ExternalId,
            "cancelled"
        );

        // Se a chamada à API foi bem-sucedida, atualiza o status localmente
        if (result.Status == "cancelled")
        {
            subscription.Status = "cancelado"; // Status local
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reativa uma assinatura pausada no Mercado Pago e no banco local.
    /// </summary>
    public async Task<bool> ReactivateSubscriptionAsync(Guid userId)
    {
        // A reativação geralmente se aplica a assinaturas pausadas
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
            s.UserId == userId && s.Status == "pausado"
        );
        if (subscription == null)
        {
            return false;
        }

        // "authorized" é o status para reativar uma assinatura
        var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
            subscription.ExternalId,
            "authorized"
        );

        if (result.Status == "authorized")
        {
            subscription.Status = "ativo"; // Ou "autorizado", conforme sua convenção local
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}
