using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs; // Namespace onde está o ChargebackNotificationPayload

// Adicione outros usings necessários (Data, Models, etc)
namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

[AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
public class ProcessChargebackJob : IJob<ChargebackNotificationPayload>
{
    private readonly ILogger<ProcessChargebackJob> _logger;
    // REMOVIDO: private readonly ApiDbContext _context;
    private readonly IChargebackRepository _chargebackRepository; // ADICIONADO
    private readonly IChargeBackNotificationService _chargeBackNotificationService;

    public ProcessChargebackJob(
        ILogger<ProcessChargebackJob> logger,
        IChargebackRepository chargebackRepository,
        IChargeBackNotificationService chargeBackNotificationService
    )
    {
        _logger = logger;
        _chargebackRepository = chargebackRepository;
        _chargeBackNotificationService = chargeBackNotificationService;
    }

    public async Task ExecuteAsync(ChargebackNotificationPayload chargebackData)
    {
        if (chargebackData == null || string.IsNullOrEmpty(chargebackData.Id))
        {
            _logger.LogError("Job de Chargeback recebido com payload inválido.");
            return;
        }

        _logger.LogInformation("Iniciando processamento do job para o Chargeback ID: {ChargebackId}", chargebackData.Id);

        try
        {
            if (!long.TryParse(chargebackData.Id, out long chargebackIdLong))
            {
                _logger.LogError("ID do Chargeback não é um número válido: {Id}", chargebackData.Id);
                return;
            }

            // --- ALTERAÇÃO: Usando Repository para verificação ---
            // Substitui: _context.Chargebacks.AsNoTracking().AnyAsync(...)
            var exists = await _chargebackRepository.ExistsByExternalIdAsync(chargebackIdLong);

            if (exists)
            {
                _logger.LogInformation("Chargeback {Id} já existe no banco. Verificando atualizações...", chargebackData.Id);
                // Continua para permitir update
            }

            await _chargeBackNotificationService.VerifyAndProcessChargeBackAsync(chargebackData);

            _logger.LogInformation("Job Chargeback {Id} concluído.", chargebackData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no Job Chargeback {Id}.", chargebackData.Id);
            throw; 
        }
    }
}