using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

public interface IPixPaymentService
{
    Task<CachedResponse> CreateIdempotentPixPaymentAsync(
        CreatePixPaymentRequest request,
        string idempotencyKey
    );
}
