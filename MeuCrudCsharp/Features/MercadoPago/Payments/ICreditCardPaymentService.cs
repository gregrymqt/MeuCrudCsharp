using MeuCrudCsharp.Services;

namespace MeuCrudCsharp.Features.MercadoPago.Payments
{
    public interface ICreditCardPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(PaymentRequestDto paymentData, Guid userId, decimal transactionAmount);
        string MapPaymentStatus(string mercadopagoStatus);
    }
}
