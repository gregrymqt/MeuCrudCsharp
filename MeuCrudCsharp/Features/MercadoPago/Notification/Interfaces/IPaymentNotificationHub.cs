using MeuCrudCsharp.Features.MercadoPago.Notification.Record;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces
{
    

    public interface IPaymentNotificationHub
    {
        Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update);
    }
}
