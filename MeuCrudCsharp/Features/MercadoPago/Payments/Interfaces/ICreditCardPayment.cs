using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface ICreditCardPayment
    {
        Task<PaymentResponseDto> CreatePaymentAsync(PaymentRequestDto paymentData, Guid userId, decimal transactionAmount);
        string MapPaymentStatus(string mercadopagoStatus);
    }
}
