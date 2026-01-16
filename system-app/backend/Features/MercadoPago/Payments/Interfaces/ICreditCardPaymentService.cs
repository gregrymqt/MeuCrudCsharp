using System.Threading.Tasks;
using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that processes credit card payments,
    /// handling both one-time payments and subscription creation.
    /// </summary>
    public interface ICreditCardPaymentService
    {
        /// <summary>
        /// Creates a new payment or a new subscription based on the request data.
        /// </summary>
        /// <param name="request">The DTO containing all necessary data for the payment, such as the card token, amount, and payer details.</param>
        /// <returns>An object representing the result of the operation, which could be a payment DTO or a subscription DTO.</returns>
        Task<CachedResponse> CreatePaymentOrSubscriptionAsync(
            CreditCardPaymentRequestDto request, 
            string idempotencyKey // 1. A assinatura do método agora inclui a idempotencyKey
        );
    }
}
