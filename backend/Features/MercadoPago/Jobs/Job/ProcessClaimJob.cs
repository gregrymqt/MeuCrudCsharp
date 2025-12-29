using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;
namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
    public class ProcessClaimJob : IJob<ClaimNotificationPayload>
    {
        private readonly ILogger<ProcessClaimJob> _logger;
        private readonly ApiDbContext _context;
        private readonly IClaimNotificationService _claimNotification;

        public ProcessClaimJob(
            ILogger<ProcessClaimJob> logger,
            ApiDbContext context,
            IClaimNotificationService claimNotification
        )
        {
            _logger = logger;
            _context = context;
            _claimNotification = claimNotification;
        }

        public async Task ExecuteAsync(ClaimNotificationPayload claimPayload)
        {
            if (claimPayload == null || string.IsNullOrEmpty(claimPayload.Id))
            {
                _logger.LogError("Job de Claim recebido com payload nulo ou ID inválido. O job será descartado.");
                return;
            }

            _logger.LogInformation("Iniciando processamento do job para a Claim ID: {ClaimId}", claimPayload.Id);

            try
            {
                // CORREÇÃO: Converter string para long antes de comparar
                if (!long.TryParse(claimPayload.Id, out long claimIdLong))
                {
                    _logger.LogError("ID inválido: {Id}", claimPayload.Id);
                    return;
                }

                // 1. Verifica a idempotência: checa se a notificação já foi registrada.
                // CORREÇÃO: Comparando long com long agora
                var existingClaim = await _context
                    .Claims.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.MpClaimId == claimIdLong);

                if (existingClaim != null)
                {
                    _logger.LogInformation("A notificação de Claim ID {ClaimId} já foi processada anteriormente.", claimPayload.Id);
                    return;
                }

                _logger.LogInformation("Notificação de Claim ID {ClaimId} é nova. Enviando para processamento.", claimPayload.Id);

                await _claimNotification.VerifyAndProcessClaimAsync(claimPayload);

                _logger.LogInformation("Processamento da Claim ID: {ClaimId} concluído com sucesso.", claimPayload.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a notificação para a Claim ID: {ClaimId}.", claimPayload.Id);
                throw;
            }
        }
    }
}