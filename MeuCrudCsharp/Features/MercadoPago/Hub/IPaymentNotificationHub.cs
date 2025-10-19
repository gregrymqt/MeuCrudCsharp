using MeuCrudCsharp.Features.MercadoPago.Notification.Record;

namespace MeuCrudCsharp.Features.MercadoPago.Hub
{
    public interface IPaymentNotificationHub
    {
        Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update);
    }
}
