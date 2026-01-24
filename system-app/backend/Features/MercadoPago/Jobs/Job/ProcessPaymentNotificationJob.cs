using Hangfire;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de pagamento do Mercado Pago.
/// Delega toda a lógica de negócio para o NotificationPaymentService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessPaymentNotificationJob(
    ILogger<ProcessPaymentNotificationJob> logger,
    INotificationPayment notificationPayment,
    ICacheService cacheService)
    : IJob<PaymentNotificationData>
{
    /// <summary>
    /// Executa o processamento da notificação de pagamento.
    /// </summary>
    /// <param name="resource">Dados da notificação de pagamento.</param>
    public async Task ExecuteAsync(PaymentNotificationData? resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.Id))
        {
            logger.LogError(
                "Job de notificação de pagamento recebido com um PaymentId nulo ou vazio. O job será descartado.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation("Iniciando processamento do job para o Payment ID: {PaymentId}", resource.Id);

        try
        {
            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar pagamento no banco
            // 2. Verificar idempotência (status já processado)
            // 3. Buscar status atualizado na API do MP
            // 4. Atualizar Payment e Subscription via Repositories
            // 5. Criar assinatura se necessário
            // 6. Commit via UnitOfWork
            // 7. Enviar email
            await notificationPayment.VerifyAndProcessNotificationAsync(resource.Id);

            logger.LogInformation("Processamento do Payment ID: {PaymentId} concluído com sucesso.", resource.Id);

            // Invalida cache após processamento bem-sucedido
            var cacheKey = $"payment:db:{resource.Id}";
            await cacheService.RemoveAsync(cacheKey);
            logger.LogInformation("Cache invalidado para a chave: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar notificação para o Payment ID: {PaymentId}.", resource.Id);
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}