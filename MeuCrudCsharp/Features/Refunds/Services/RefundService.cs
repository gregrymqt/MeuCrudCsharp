// Local: Features/Refunds/Services/RefundService.cs

using System.Security.Claims;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base; // Importando a classe base
using MeuCrudCsharp.Features.Refunds.DTOs;
using MeuCrudCsharp.Features.Refunds.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Refunds.Services
{
    // --- CORREÇÃO: Herda da classe base e implementa a interface ---
    public class RefundService : MercadoPagoServiceBase, IRefundService
    {
        private readonly ApiDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // --- CORREÇÃO: Construtor ajustado para injetar dependências corretas ---
        public RefundService(
            ApiDbContext context,
            IHttpContextAccessor httpContextAccessor,
            HttpClient httpClient,
            ILogger<RefundService> logger
        )
            : base(httpClient, logger) // Passa dependências para a classe base
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RequestUserRefundAsync()
        {
            var userId = GetCurrentUserId();

            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.Status == "ativo"
            );

            if (subscription == null)
            {
                throw new AppServiceException(
                    "Nenhuma assinatura ativa encontrada para solicitar reembolso."
                );
            }

            var payment = await _context
                .Payments.Where(p => p.subscription_id == subscription.Id && p.Status == "approved")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                throw new AppServiceException(
                    "Nenhum pagamento aprovado encontrado para esta assinatura."
                );
            }

            // 2. Validar a regra de negócio (7 dias)
            if (payment.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                throw new AppServiceException(
                    "O prazo de 7 dias para solicitação de reembolso expirou."
                );
            }

            await RefundPaymentOnMercadoPagoAsync(payment.PaymentId);

            subscription.Status = "blocked";
            _context.Subscriptions.Update(subscription);

            _context.Payments.Remove(payment);

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Reembolso processado e dados locais atualizados para o usuário {UserId}",
                userId
            );
        }

        private async Task RefundPaymentOnMercadoPagoAsync(string paymentId, decimal? amount = null)
        {
            _logger.LogInformation(
                "Iniciando reembolso no Mercado Pago para o pagamento: {PaymentId}",
                paymentId
            );

            var endpoint = $"/v1/payments/{paymentId}/refunds";
            var payload = new RefundRequestDto { Amount = amount }; // Reembolso total se o valor for nulo

            await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        }

        private Guid GetCurrentUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido.");
            }
            return userId;
        }
    }
}
