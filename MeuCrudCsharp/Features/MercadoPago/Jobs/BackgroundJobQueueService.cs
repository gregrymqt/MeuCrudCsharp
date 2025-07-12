// Em Services/BackgroundJobQueueService.cs (Exemplo de implementação)

using Hangfire;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    // Esta é a implementação real da interface que seu controller usa.
    public class BackgroundJobQueueService : IQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public BackgroundJobQueueService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public Task EnqueuePaymentNotificationAsync(string paymentId)
        {
            // Apenas enfileira o trabalho para ser executado em segundo plano.
            // O Hangfire irá instanciar a classe 'ProcessPaymentNotificationJob'
            // e chamar o método 'ExecuteAsync' com o 'paymentId'.
            _backgroundJobClient.Enqueue<ProcessPaymentNotificationJob>(job => job.ExecuteAsync(paymentId));

            // Retorna imediatamente.
            return Task.CompletedTask;
        }
    }
}