using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar renovação de assinatura.
/// Delega toda a lógica de negócio para o SubscriptionRenewalNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessRenewalSubscriptionJob(
    ILogger<ProcessRenewalSubscriptionJob> logger,
    ISubscriptionNotificationService subscriptionNotificationService)
    : IJob<PaymentNotificationData>
{
    /// <summary>
    /// Executa o processamento da renovação de assinatura.
    /// </summary>
    public async Task ExecuteAsync(PaymentNotificationData? resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.Id))
        {
            logger.LogError("Job recebido com ResourceId nulo ou vazio. O job será descartado.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation(
            "Iniciando o processamento da renovação de assinatura com PaymentId: {PaymentId}",
            resource.Id
        );

        try
        {
            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar assinatura pelo PaymentId via Repository
            // 2. Verificar idempotência (data de expiração)
            // 3. Calcular nova data de expiração
            // 4. Atualizar assinatura via Repository
            // 5. Commit via UnitOfWork
            // 6. Enviar email
            await subscriptionNotificationService.ProcessRenewalAsync(resource.Id);

            logger.LogInformation(
                "Renovação da assinatura com PaymentId: {PaymentId} concluída com sucesso.",
                resource.Id
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar renovação da assinatura com PaymentId: {PaymentId}",
                resource.Id
            );
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}
