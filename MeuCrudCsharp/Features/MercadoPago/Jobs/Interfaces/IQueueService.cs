using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

// Adicione este using

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces
{
    public interface IQueueService
    {
        // O método antigo foi removido e substituído por este método genérico.
        // O "where TJob : IJob" garante que só podemos passar classes que implementam nossa interface IJob.
        Task EnqueueJobAsync<TJob>(string resourceId) where TJob : IJob;
    }
}