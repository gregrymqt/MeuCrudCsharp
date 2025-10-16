namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces
{
    /// <summary>
    /// Define o contrato para um job que pode ser enfileirado e executado em segundo plano.
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Executa a lógica do job.
        /// </summary>
        /// <param name="resourceId">O ID do recurso a ser processado (ex: paymentId, subscriptionId).</param>
        Task ExecuteAsync(string resourceId);
    }
}