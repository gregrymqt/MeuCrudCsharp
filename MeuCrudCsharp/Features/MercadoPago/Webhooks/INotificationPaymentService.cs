namespace MeuCrudCsharp.Features.MercadoPago.Webhooks
{
    public interface INotificationPaymentService
    {
        Task VerifyAndProcessNotificationAsync(string paymentId);
    }
}