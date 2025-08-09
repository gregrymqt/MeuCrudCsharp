using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    /// <summary>
    /// Define o contrato para um serviço que enfileira tarefas em segundo plano.
    /// Abstrai a implementação específica do sistema de filas (ex: Hangfire, RabbitMQ).
    /// </summary>
    public interface IQueueService
    {
        /// <summary>
        /// Enfileira um job para processar uma notificação de pagamento de forma assíncrona.
        /// </summary>
        /// <param name="paymentId">O identificador do pagamento a ser processado.</param>
        /// <returns>Uma <see cref="Task"/> que representa a conclusão da operação de enfileiramento.</returns>
        Task EnqueuePaymentNotificationAsync(string paymentId);
    }
}
