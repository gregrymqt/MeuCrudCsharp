using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Resource.Preference;
using MeuCrudCsharp.Features.Exceptions;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that creates payment preferences in Mercado Pago.
    /// </summary>
    public interface IPreferencePayment
    {
        /// <summary>
        /// Creates a new payment preference for a given amount and user.
        /// </summary>
        /// <param name="amount">The total amount for the payment preference.</param>
        /// <param name="user">The authenticated user's claims, used to retrieve payer information.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Preference"/> object from the Mercado Pago SDK.</returns>
        /// <exception cref="AppServiceException">Thrown for internal application errors, such as failing to find the user.</exception>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the Mercado Pago API.</exception>
        Task<Preference> CreatePreferenceAsync(decimal amount, ClaimsPrincipal user);
    }
}
