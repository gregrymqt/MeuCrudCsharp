// Crie um novo arquivo, ex: Services/IQueueService.cs
namespace MeuCrudCsharp.Services
{
    public interface IQueueService
    {
        Task EnqueuePaymentNotificationAsync(string paymentId);
    }
}