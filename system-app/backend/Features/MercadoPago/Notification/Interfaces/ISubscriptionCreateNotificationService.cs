namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

public interface ISubscriptionCreateNotificationService
{
    Task VerifyAndProcessSubscriptionAsync(string externalId);
}
