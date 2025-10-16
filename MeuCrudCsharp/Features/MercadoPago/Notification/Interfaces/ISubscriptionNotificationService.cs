using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces
{
    public interface ISubscriptionNotificationService
    {
        /// <summary>
        /// Processa a renovação de uma assinatura após um pagamento ser autorizado.
        /// </summary>
        /// <param name="subscription">A entidade da assinatura a ser renovada.</param>
        /// <returns>Task.</returns>
        Task ProcessRenewalAsync(Subscription subscription);
    }
}