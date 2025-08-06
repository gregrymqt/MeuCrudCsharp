using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
    public class RefundService
    {
        private readonly ApiDbContext _context;
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefundService(ApiDbContext context, MercadoPagoService mercadoPagoService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Orquestra o processo de solicitação de reembolso para o usuário logado.
        /// </summary>
        public async Task RequestUserRefundAsync()
        {
            var userId = GetCurrentUserId();

            // 1. Encontrar a assinatura e o pagamento elegível
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ativo");

            if (subscription == null)
            {
                throw new InvalidOperationException("Nenhuma assinatura ativa encontrada para solicitar reembolso.");
            }

            var payment = await _context.Payments
                .Where(p => p.subscription_id == subscription.Id && p.Status == "approved")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                throw new InvalidOperationException("Nenhum pagamento aprovado encontrado para esta assinatura.");
            }

            // 2. Validar a regra de negócio (7 dias)
            if (payment.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                throw new InvalidOperationException("O prazo de 7 dias para solicitação de reembolso expirou.");
            }

            // 3. Chamar o serviço do Mercado Pago para efetuar o reembolso
            await _mercadoPagoService.RefundPaymentAsync(payment.PaymentId);

            // 4. Atualizar o banco de dados local
            // Marca a assinatura como bloqueada/cancelada
            subscription.Status = "blocked";
            _context.Subscriptions.Update(subscription);

            // Remove o registro do pagamento do histórico
            _context.Payments.Remove(payment);

            await _context.SaveChangesAsync();
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Usuário não autenticado.");
            }
            return Guid.Parse(userId);
        }
    }
}