using Hangfire;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Services;

public class BackgroundJobQueueService : IQueueService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<BackgroundJobQueueService> _logger;

    public BackgroundJobQueueService(
        IBackgroundJobClient backgroundJobClient,
        ILogger<BackgroundJobQueueService> logger
    )
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    // MÉTODO GENÉRICO QUE SUBSTITUI O ANTIGO
    public Task EnqueueJobAsync<TJob>(string resourceId) where TJob : IJob
    {
        if (string.IsNullOrEmpty(resourceId))
        {
            throw new ArgumentException("O ID do recurso não pode ser nulo ou vazio.", nameof(resourceId));
        }

        try
        {
            var jobName = typeof(TJob).Name;
            _logger.LogInformation(
                "Enfileirando job do tipo '{JobName}' para o ResourceId: {ResourceId}",
                jobName,
                resourceId
            );

            // A mágica acontece aqui: TJob é o tipo genérico que representa a classe do job
            _backgroundJobClient.Enqueue<TJob>(job => job.ExecuteAsync(resourceId));

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao enfileirar o job para o ResourceId {ResourceId}. O job NÃO foi agendado.",
                resourceId
            );

            throw new AppServiceException("Falha ao agendar a tarefa de processamento.", ex);
        }
    }
}