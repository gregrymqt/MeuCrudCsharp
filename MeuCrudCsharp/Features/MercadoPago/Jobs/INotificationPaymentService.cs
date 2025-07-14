namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public interface INotificationPaymentService
    {
        Task VerifyAndProcessNotificationAsync(string paymentId);
    }
}
