using Hangfire;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Services;

public class BackgroundJobQueueService(
    IBackgroundJobClient backgroundJobClient,
    ILogger<BackgroundJobQueueService> logger)
    : IQueueService
{
    /// <summary>
    /// Enfileira um job genérico no Hangfire para execução em segundo plano.
    /// </summary>
    /// <typeparam name="TJob">O tipo do job a ser executado, que deve implementar IJob&lt;TResource&gt;.</typeparam>
    /// <typeparam name="TResource">O tipo do payload (recurso) que o job processará.</typeparam>
    /// <param name="resource">O payload a ser passado para o método ExecuteAsync do job.</param>
    public Task EnqueueJobAsync<TJob, TResource>(TResource resource)
        where TJob : IJob<TResource>
    {
        if (resource == null)
        {
            throw new ArgumentNullException(
                nameof(resource),
                "O recurso (payload) não pode ser nulo."
            );
        }

        try
        {
            var jobName = typeof(TJob).Name;
            logger.LogInformation(
                "Enfileirando job do tipo '{JobName}' com o payload: {Payload}",
                jobName,
                resource
            );

            // O Hangfire serializa a chamada para o método ExecuteAsync com o payload do tipo TResource.
            backgroundJobClient.Enqueue<TJob>(job => job.ExecuteAsync(resource));

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao enfileirar o job do tipo {JobName}. O job NÃO foi agendado.",
                typeof(TJob).Name
            );
            throw new AppServiceException("Falha ao agendar a tarefa de processamento.", ex);
        }
    }
}
