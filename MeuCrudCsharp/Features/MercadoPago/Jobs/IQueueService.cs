namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public interface IQueueService
    {
        Task EnqueuePaymentNotificationAsync(string paymentId);
    }
}