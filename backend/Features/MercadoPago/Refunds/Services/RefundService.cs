using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Refunds.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.User.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Services
{
    public class RefundService : MercadoPagoServiceBase, IRefundService
    {
        private readonly ApiDbContext _context;
        private readonly IUserContext _userContext;

        public RefundService(
            ApiDbContext context,
            IHttpClientFactory httpClient,
            ILogger<RefundService> logger,
            IUserContext userContext
        )
            : base(httpClient, logger)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task RequestUserRefundAsync()
        {
            var userId = await _userContext.GetCurrentUserId();

            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.Status == "ativo"
            );

            if (subscription == null)
            {
                throw new AppServiceException("No active subscription found to request a refund.");
            }

            var payment = await _context
                .Payments.Where(p => p.SubscriptionId == subscription.Id && p.Status == "aprovada")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                throw new AppServiceException("No approved payment found for this subscription.");
            }

            if (payment.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                throw new AppServiceException("The 7-day period for a refund request has expired.");
            }

            try
            {
                await RefundPaymentOnMercadoPagoAsync(payment.ExternalId);

                subscription.Status = "refund_pending";
                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Refund request initiated for user {UserId}. Waiting for webhook confirmation.",
                    userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process refund on Mercado Pago for payment {ExternalId}. Database changes were not saved.",
                    payment.ExternalId
                );

                throw new AppServiceException(
                    "An error occurred while communicating with the payment provider. Please try again later.",
                    ex
                );
            }
        }

        private async Task RefundPaymentOnMercadoPagoAsync(
            string externalPaymentId,
            decimal? amount = null
        )
        {
            _logger.LogInformation(
                "Initiating refund on Mercado Pago for payment: {PaymentId}",
                externalPaymentId
            );

            var endpoint = $"/v1/payments/{externalPaymentId}/refunds";
            var payload = new RefundRequestDto(amount);

            await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        }
    }
}
