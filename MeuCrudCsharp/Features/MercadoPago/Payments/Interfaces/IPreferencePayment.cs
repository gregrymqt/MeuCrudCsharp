using MeuCrudCsharp.Models;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface IPreferencePayment
    {
        Task<PaymentRequestDto> createPreference(decimal amount,Users user);
    }
}
