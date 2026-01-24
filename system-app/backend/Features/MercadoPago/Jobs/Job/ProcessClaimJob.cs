using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de Claims do Mercado Pago.
/// Delega toda a lógica de negócio para o ClaimNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessClaimJob(
    ILogger<ProcessClaimJob> logger,
    IClaimNotificationService claimNotification)
    : IJob<ClaimNotificationPayload>
{
    /// <summary>
    /// Executa o processamento da notificação de claim.
    /// </summary>
    public async Task ExecuteAsync(ClaimNotificationPayload? claimPayload)
    {
        if (claimPayload == null || string.IsNullOrEmpty(claimPayload.Id))
        {
            logger.LogError("Job de Claim recebido com payload nulo ou ID inválido. O job será descartado.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation("Iniciando processamento do job para a Claim ID: {ClaimId}", claimPayload.Id);

        try
        {
            // Validação de formato do ID
            if (!long.TryParse(claimPayload.Id, out _))
            {
                logger.LogError("ID da Claim não é um número válido: {Id}", claimPayload.Id);
                return; // Não relança para evitar retentativas desnecessárias
            }

            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar detalhes na API do MP
            // 2. Verificar se claim já existe (idempotência)
            // 3. Localizar usuário via Payment ou Subscription
            // 4. Criar/Atualizar Claim via Repository
            // 5. Commit via UnitOfWork
            // 6. Enviar email
            await claimNotification.VerifyAndProcessClaimAsync(claimPayload);

            logger.LogInformation("Processamento da Claim ID: {ClaimId} concluído com sucesso.", claimPayload.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar a notificação para a Claim ID: {ClaimId}.", claimPayload.Id);
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}