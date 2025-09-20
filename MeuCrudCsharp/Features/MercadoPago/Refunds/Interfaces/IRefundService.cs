using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles refund requests.
    /// </summary>
    public interface IRefundService
    {
        /// <summary>
        /// Initiates and processes a refund request for the currently authenticated user.
        /// </summary>
        /// <returns>A task that represents the asynchronous refund operation.</returns>
        Task RequestUserRefundAsync();
    }
}
