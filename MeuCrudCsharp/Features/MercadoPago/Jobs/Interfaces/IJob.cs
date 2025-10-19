namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces
{
    /// <summary>
    /// Define o contrato para um job que pode ser enfileirado e executado em segundo plano,
    /// processando um recurso de um tipo específico.
    /// </summary>
    /// <typeparam name="TResource">O tipo do recurso que o job irá processar.</typeparam>
    public interface IJob<in TResource>
    {
        /// <summary>
        /// Executa a lógica do job.
        /// </summary>
        /// <param name="resource">O recurso a ser processado.</param>
        Task ExecuteAsync(TResource resource);
    }
}