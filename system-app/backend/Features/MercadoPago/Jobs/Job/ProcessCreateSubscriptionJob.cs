using Hangfire;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar criação de assinatura.
/// Delega toda a lógica de negócio para o SubscriptionCreateNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessCreateSubscriptionJob(
    ILogger<ProcessCreateSubscriptionJob> logger,
    ICacheService cache,
    ISubscriptionCreateNotificationService notificationSubscriptionCreate)
    : IJob<PaymentNotificationData>
{
    /// <summary>
    /// Executa o processamento da criação de assinatura.
    /// </summary>
    public async Task ExecuteAsync(PaymentNotificationData? resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.Id))
        {
            logger.LogError("Job recebido com ResourceId nulo ou vazio. O job será descartado.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation(
            "Iniciando processamento do job para a criação de assinatura: {ResourceId}",
            resource.Id
        );

        try
        {
            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar assinatura no banco via Repository
            // 2. Verificar idempotência (status)
            // 3. Atualizar status da assinatura
            // 4. Commit via UnitOfWork
            // 5. Enviar email
            await notificationSubscriptionCreate.VerifyAndProcessSubscriptionAsync(resource.Id);

            logger.LogInformation(
                "Processamento da criação de assinatura ID: {ResourceId} concluído com sucesso.",
                resource.Id
            );

            // Invalida cache após sucesso
            var cacheKey = $"subscription_{resource.Id}";
            await cache.RemoveAsync(cacheKey);
            logger.LogInformation("Cache invalidado para a chave: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar a criação de assinatura ID: {ResourceId}", resource.Id);
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}
