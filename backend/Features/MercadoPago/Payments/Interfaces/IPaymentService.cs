using System;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

public interface IPaymentService
{
    // Adicione este método à interface existente
    Task<List<PaymentHistoryDto>> GetUserPaymentHistoryAsync(string userId);
}
