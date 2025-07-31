// Features/MercadoPago/Payments/Interfaces/ICreditCardPayments.cs
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface ICreditCardPayments
    {
        // Este é o único método que precisa ser público no contrato.
        Task<object> CreatePaymentOrSubscriptionAsync(PaymentRequestDto request);

        string MapPaymentStatus(string mercadopagoStatus);
    }
}
