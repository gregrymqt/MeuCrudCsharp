using MeuCrudCsharp.Features.MercadoPago.Notification.Record;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces
{
    

    public interface IPaymentNotificationService
    {
        Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update);
    }
}
