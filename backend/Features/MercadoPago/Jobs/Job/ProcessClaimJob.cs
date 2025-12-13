using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de 'claims' (disputas, reclamações, etc.).
/// </summary>
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

    /// <summary>
    /// Executa o processamento da notificação de claim.
    /// </summary>
    /// <param name="claimPayload">O payload da notificação recebida do webhook.</param>
    public async Task ExecuteAsync(ClaimNotificationPayload claimPayload)
    {
        if (claimPayload == null || claimPayload.Id == 0)
        {
            _logger.LogError(
                "Job de Claim recebido com payload nulo ou ID inválido. O job será descartado."
            );
            // Não relança a exceção para evitar retentativas desnecessárias.
            return;
        }

        _logger.LogInformation(
            "Iniciando processamento do job para a Claim ID: {ClaimId}",
            claimPayload.Id
        );

        try
        {
            // 1. Verifica a idempotência: checa se a notificação já foi registrada.
            var existingClaim = await _context
                .Claims.AsNoTracking()
                .FirstOrDefaultAsync(c => c.NotificationId == claimPayload.Id);

            if (existingClaim != null)
            {
                _logger.LogInformation(
                    "A notificação de Claim ID {ClaimId} já foi processada anteriormente. Finalizando job com sucesso.",
                    claimPayload.Id
                );
                return; // Operação já concluída, encerra o job.
            }

            // 2. Delega para o serviço especializado realizar a lógica de negócio.
            // Este serviço será responsável por buscar os detalhes da claim na API do MP
            // e salvar o registro final no banco de dados.
            _logger.LogInformation(
                "Notificação de Claim ID {ClaimId} é nova. Enviando para processamento.",
                claimPayload.Id
            );
            await _claimNotification.VerifyAndProcessClaimAsync(claimPayload);

            _logger.LogInformation(
                "Processamento da Claim ID: {ClaimId} concluído com sucesso.",
                claimPayload.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar a notificação para a Claim ID: {ClaimId}. O Hangfire irá tentar novamente.",
                claimPayload.Id
            );
            throw; // Relança a exceção para que o Hangfire aplique a política de retentativas.
        }
    }
}
