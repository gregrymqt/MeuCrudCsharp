using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
    public class ProcessClaimJob(
        ILogger<ProcessClaimJob> logger,
        ApiDbContext context,
        IClaimNotificationService claimNotification)
        : IJob<ClaimNotificationPayload>
    {
        public async Task ExecuteAsync(ClaimNotificationPayload? claimPayload)
        {
            if (claimPayload == null || string.IsNullOrEmpty(claimPayload.Id))
            {
                logger.LogError("Job de Claim recebido com payload nulo ou ID inválido. O job será descartado.");
                return;
            }

            logger.LogInformation("Iniciando processamento do job para a Claim ID: {ClaimId}", claimPayload.Id);

            try
            {
                // CORREÇÃO: Converter string para long antes de comparar
                if (!long.TryParse(claimPayload.Id, out var claimIdLong))
                {
                    logger.LogError("ID inválido: {Id}", claimPayload.Id);
                    return;
                }

                // 1. Verifica a idempotência: checa se a notificação já foi registrada.
                // CORREÇÃO: Comparando long com long agora
                var existingClaim = await context
                    .Claims.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.MpClaimId == claimIdLong);

                if (existingClaim != null)
                {
                    logger.LogInformation("A notificação de Claim ID {ClaimId} já foi processada anteriormente.",
                        claimPayload.Id);
                    return;
                }

                logger.LogInformation("Notificação de Claim ID {ClaimId} é nova. Enviando para processamento.",
                    claimPayload.Id);

                await claimNotification.VerifyAndProcessClaimAsync(claimPayload);

                logger.LogInformation("Processamento da Claim ID: {ClaimId} concluído com sucesso.", claimPayload.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar a notificação para a Claim ID: {ClaimId}.", claimPayload.Id);
                throw;
            }
        }
    }
}