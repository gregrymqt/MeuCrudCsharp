using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de atualização de plano de assinatura.
/// Delega toda a lógica de negócio para o PlanUpdateNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessPlanSubscriptionJob(
    ILogger<ProcessPlanSubscriptionJob> logger,
    IPlanUpdateNotificationService planUpdateNotificationService)
    : IJob<PaymentNotificationData>
{
    /// <summary>
    /// Executa o processamento da notificação de atualização de plano.
    /// </summary>
    public async Task ExecuteAsync(PaymentNotificationData? resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.Id))
        {
            logger.LogWarning("Job recebido com ID de plano nulo ou vazio. O job será descartado.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation(
            "Iniciando processamento do job para o Plano ExternalId: {ExternalId}",
            resource.Id
        );

        try
        {
            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar plano na API do MP
            // 2. Buscar plano no banco via Repository
            // 3. Comparar valores e detectar mudanças
            // 4. Atualizar plano via Repository
            // 5. Commit via UnitOfWork
            // 6. Enviar email ao admin
            await planUpdateNotificationService.VerifyAndProcessPlanUpdate(resource.Id);

            logger.LogInformation(
                "Processamento do Plano ExternalId: {ExternalId} concluído com sucesso.",
                resource.Id
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar assinatura do plano com ExternalId {ExternalId}.",
                resource.Id
            );
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}
