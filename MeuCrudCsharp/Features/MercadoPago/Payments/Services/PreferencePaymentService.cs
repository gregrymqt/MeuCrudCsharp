using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class PreferencePaymentService : IPreferencePayment
    {
        public async Task<PaymentRequestDto> createPreference(decimal amount, Users user)
        {

        }

    }
}
