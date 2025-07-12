namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces
{
    public interface INotificationPaymentService
    {
        Task VerifyAndProcessNotificationAsync(string paymentId);
    }
}