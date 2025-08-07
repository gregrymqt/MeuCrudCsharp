using System;
using System.Threading.Tasks;
using Hangfire;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções customizadas
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public class BackgroundJobQueueService : IQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<BackgroundJobQueueService> _logger; // MUDANÇA 1: Injetando o Logger

        public BackgroundJobQueueService(
            IBackgroundJobClient backgroundJobClient,
            ILogger<BackgroundJobQueueService> logger
        ) // MUDANÇA 1
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public Task EnqueuePaymentNotificationAsync(string paymentId)
        {
            // MUDANÇA 2: Validação "Fail-Fast"
            if (string.IsNullOrEmpty(paymentId))
            {
                throw new ArgumentException(
                    "O ID do pagamento não pode ser nulo ou vazio.",
                    nameof(paymentId)
                );
            }

            // MUDANÇA 3: Bloco try-catch para garantir que o enfileiramento funcione
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
                // MUDANÇA 4: Logging detalhado e lançamento de uma exceção customizada
                _logger.LogError(
                    ex,
                    "Falha ao enfileirar o job de notificação de pagamento para o PaymentId {PaymentId}. O job NÃO foi agendado.",
                    paymentId
                );

                // Lança nossa exceção de serviço para que a camada que chamou (ex: o WebhookController)
                // saiba que a operação falhou e possa retornar um erro HTTP 500.
                throw new AppServiceException(
                    "Falha ao agendar a tarefa de processamento de pagamento.",
                    ex
                );
            }
        }
    }
}
