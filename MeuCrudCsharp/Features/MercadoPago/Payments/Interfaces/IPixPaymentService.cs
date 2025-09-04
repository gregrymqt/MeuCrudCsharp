using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

    public interface IPixPaymentService
    {
        Task<PaymentResponseDto> CreatePixPaymentAsync(string userId, CreatePixPaymentRequest request);
    }
