using System;
using System.Threading.Tasks;
using Hangfire;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    /// <summary>
    /// Implementação de <see cref="IQueueService"/> que utiliza o Hangfire para enfileirar
    /// tarefas em segundo plano.
    /// </summary>
    public class BackgroundJobQueueService : IQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<BackgroundJobQueueService> _logger;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="BackgroundJobQueueService"/>.
        /// </summary>
        /// <param name="backgroundJobClient">O cliente do Hangfire para enfileirar os jobs.</param>
        /// <param name="logger">O serviço de logging para registrar informações e erros.</param>
        public BackgroundJobQueueService(
            IBackgroundJobClient backgroundJobClient,
            ILogger<BackgroundJobQueueService> logger
        )
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        /// <summary>
        /// Enfileira um job para processar uma notificação de pagamento de forma assíncrona.
        /// </summary>
        /// <param name="paymentId">O identificador do pagamento a ser processado.</param>
        /// <returns>Uma <see cref="Task"/> que representa a conclusão da operação de enfileiramento.</returns>
        /// <exception cref="ArgumentException">Lançada se o <paramref name="paymentId"/> for nulo ou vazio.</exception>
        /// <exception cref="AppServiceException">Lançada se ocorrer uma falha ao tentar enfileirar o job no Hangfire.</exception>
        public Task EnqueuePaymentNotificationAsync(string paymentId)
        {
            if (string.IsNullOrEmpty(paymentId))
            {
                throw new ArgumentException(
                    "O ID do pagamento não pode ser nulo ou vazio.",
                    nameof(paymentId)
                );
            }

            try
            {
                _logger.LogInformation(
                    "Enfileirando job de processamento para o PaymentId: {PaymentId}",
                    paymentId
                );

                _backgroundJobClient.Enqueue<ProcessPaymentNotificationJob>(job =>
                    job.ExecuteAsync(paymentId)
                );

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enfileirar o job de notificação de pagamento para o PaymentId {PaymentId}. O job NÃO foi agendado.",
                    paymentId
                );

                throw new AppServiceException(
                    "Falha ao agendar a tarefa de processamento de pagamento.",
                    ex
                );
            }
        }
    }
}
