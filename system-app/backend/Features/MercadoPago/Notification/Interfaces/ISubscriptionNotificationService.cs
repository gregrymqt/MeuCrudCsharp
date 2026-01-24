namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

public interface ISubscriptionNotificationService
{
    /// <summary>
    /// Processa a renovação de uma assinatura após um pagamento ser autorizado.
    /// </summary>
    /// <param name="paymentId">ID do pagamento que renovou a assinatura.</param>
    /// <returns>Task.</returns>
    Task ProcessRenewalAsync(string paymentId);
}
