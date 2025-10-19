using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de chargebacks.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
public class ProcessChargebackJob : IJob<ChargebackNotificationPayload>
{
    private readonly ILogger<ProcessChargebackJob> _logger;
    private readonly ApiDbContext _context;
    private readonly IChargeBackNotificationService _chargeBackNotificationService;

    public ProcessChargebackJob(
        ILogger<ProcessChargebackJob> logger,
        ApiDbContext context,
        IChargeBackNotificationService chargeBackNotificationService
    )
    {
        _logger = logger;
        _context = context;
        _chargeBackNotificationService = chargeBackNotificationService;
    }

    /// <summary>
    /// Executa o processamento da notificação de chargeback.
    /// </summary>
    /// <param name="chargebackData">O payload da notificação recebida do webhook.</param>
    public async Task ExecuteAsync(ChargebackNotificationPayload chargebackData)
    {
        if (chargebackData == null || chargebackData.Id == 0)
        {
            _logger.LogError(
                "Job de Chargeback recebido com payload nulo ou ID inválido. O job será descartado."
            );
            // Não relança a exceção para evitar retentativas desnecessárias.
            return;
        }

        _logger.LogInformation(
            "Iniciando processamento do job para o Chargeback ID: {ChargebackId}",
            chargebackData.Id
        );

        try
        {
            // 1. Verifica a idempotência: checa se o chargeback já foi registrado.
            var existingChargeback = await _context
                .Chargebacks.AsNoTracking()
                .AnyAsync(c => c.ChargebackId == chargebackData.Id);

            if (existingChargeback)
            {
                _logger.LogInformation(
                    "O Chargeback ID {ChargebackId} já foi processado anteriormente. Finalizando job.",
                    chargebackData.Id
                );
                return; // Operação já concluída, encerra o job.
            }

            // 2. Delega para o serviço especializado realizar a lógica de negócio.
            await _chargeBackNotificationService.VerifyAndProcessChargeBackAsync(chargebackData);

            _logger.LogInformation(
                "Processamento do Chargeback ID: {ChargebackId} concluído com sucesso.",
                chargebackData.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar a notificação para o Chargeback ID: {ChargebackId}. O Hangfire irá tentar novamente.",
                chargebackData.Id
            );
            throw; // Relança a exceção para que o Hangfire aplique a política de retentativas.
        }
    }
}
