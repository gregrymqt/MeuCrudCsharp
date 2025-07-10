// Crie um novo arquivo, ex: Services/IQueueService.cs
namespace MeuCrudCsharp.Services
{
    public interface INotificationPaymentService
    {
        Task VerifyAndProcessNotificationAsync(string paymentId);
    }
}