namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces
{
    public interface IQueueService
    {
        /// <summary>
        /// Enfileira um job para execução em segundo plano.
        /// </summary>
        /// <typeparam name="TJob">O tipo do job a ser executado.</typeparam>
        /// <param name="resource">O recurso (parâmetro) a ser passado para o método ExecuteAsync do job.</param>
        Task EnqueueJobAsync<TJob, TResource>(TResource resource)
            where TJob : IJob<TResource>;
    }
}
