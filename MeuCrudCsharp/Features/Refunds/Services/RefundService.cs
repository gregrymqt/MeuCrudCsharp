using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.Refunds.DTOs;
using MeuCrudCsharp.Features.Refunds.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Refunds.Services
{
    /// <summary>
    /// Implements <see cref="IRefundService"/> to handle user-initiated refund requests.
    /// This service coordinates between the local database and the Mercado Pago API to process refunds.
    /// </summary>
    public class RefundService : MercadoPagoServiceBase, IRefundService
    {
        private readonly ApiDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefundService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="httpContextAccessor">Accessor to the current HTTP context to retrieve user claims.</param>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
        public RefundService(
            ApiDbContext context,
            IHttpContextAccessor httpContextAccessor,
            HttpClient httpClient,
            ILogger<RefundService> logger
        )
            : base(httpClient, logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes a refund request for the currently authenticated user's last payment.
        /// </summary>
        /// <remarks>
        /// This method enforces several business rules:
        /// 1. The user must have an active subscription.
        /// 2. The subscription must have at least one 'approved' payment.
        /// 3. The last approved payment must be within the 7-day refund window.
        /// On successful refund, the user's subscription is 'blocked' and the local payment record is removed.
        /// </remarks>
        /// <exception cref="AppServiceException">Thrown if a business rule is violated (e.g., no active subscription, refund period expired).</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        public async Task RequestUserRefundAsync()
        {
            var userId = GetCurrentUserId();

            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.Status == "active"
            );

            if (subscription == null)
            {
                throw new AppServiceException("No active subscription found to request a refund.");
            }

            var payment = await _context
                .Payments.Where(p => p.SubscriptionId == subscription.Id && p.Status == "approved")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                throw new AppServiceException("No approved payment found for this subscription.");
            }

            // Validate the 7-day business rule for refunds.
            if (payment.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                throw new AppServiceException("The 7-day period for a refund request has expired.");
            }

            await RefundPaymentOnMercadoPagoAsync(payment.ExternalId);

            subscription.Status = "refund_pending";
            _context.Subscriptions.Update(subscription);

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Refund request initiated for user {UserId}. Waiting for webhook confirmation.",
                userId
            );
        }

        /// <summary>
        /// Sends a refund request to the Mercado Pago API for a specific payment.
        /// </summary>
        /// <param name="externalPaymentId">The external identifier of the payment to be refunded.</param>
        /// <param name="amount">The amount to refund. If null, a full refund is processed.</param>
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
            var payload = new RefundRequestDto { Amount = amount }; // Full refund if amount is null.

            await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        }

        /// <summary>
        /// Safely retrieves the current user's unique identifier from the security claims.
        /// </summary>
        /// <returns>The GUID of the authenticated user.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user's identifier claim is missing or invalid.</exception>
        private string GetCurrentUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );
            if (string.IsNullOrEmpty(userIdStr))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userIdStr;
        }
    }
}
