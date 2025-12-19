using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Services;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs; // Namespace onde está o ChargebackNotificationPayload
using Microsoft.EntityFrameworkCore;

// Adicione outros usings necessários (Data, Models, etc)

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

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

    public async Task ExecuteAsync(ChargebackNotificationPayload chargebackData)
    {
        // CORREÇÃO: Verificação de string nula ou vazia
        if (chargebackData == null || string.IsNullOrEmpty(chargebackData.Id))
        {
            _logger.LogError("Job de Chargeback recebido com payload inválido.");
            return;
        }

        _logger.LogInformation(
            "Iniciando processamento do job para o Chargeback ID: {ChargebackId}",
            chargebackData.Id
        );

        try
        {
            // CORREÇÃO: Conversão de string para long para consultar no banco
            if (!long.TryParse(chargebackData.Id, out long chargebackIdLong))
            {
                _logger.LogError(
                    "ID do Chargeback não é um número válido: {Id}",
                    chargebackData.Id
                );
                return;
            }

            // 1. Verifica idempotência
            var existingChargeback = await _context
                .Chargebacks.AsNoTracking()
                .AnyAsync(c => c.ChargebackId == chargebackIdLong);

            if (existingChargeback)
            {
                // Aqui decidimos se paramos ou se atualizamos.
                // Se for só "criação", paramos. Se quiser atualizar status, deixe passar.
                _logger.LogInformation(
                    "Chargeback {Id} já existe no banco. Verificando atualizações...",
                    chargebackData.Id
                );
                // Vamos deixar passar para o Service decidir se atualiza algo
            }

            // 2. Delega para o serviço
            await _chargeBackNotificationService.VerifyAndProcessChargeBackAsync(chargebackData);

            _logger.LogInformation("Job Chargeback {Id} concluído.", chargebackData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no Job Chargeback {Id}.", chargebackData.Id);
            throw; // Hangfire tenta de novo
        }
    }
}
