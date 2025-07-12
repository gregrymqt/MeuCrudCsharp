using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface ICreditCardPayment
    {
        Task<PaymentResponseDto> CreatePaymentAsync(
            PaymentRequestDto paymentData,
            decimal transactionAmount
        );
        string MapPaymentStatus(string mercadopagoStatus);
    }
}
