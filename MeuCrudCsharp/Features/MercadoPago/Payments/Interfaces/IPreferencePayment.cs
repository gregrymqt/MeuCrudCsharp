using System.Security.Claims;
using MercadoPago.Resource.Preference;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface IPreferencePayment
    {
        Task<Preference> CreatePreferenceAsync(decimal amount, ClaimsPrincipal user);
    }
}
