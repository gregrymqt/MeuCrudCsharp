using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de Chargeback do Mercado Pago.
/// Delega toda a lógica de negócio para o ChargeBackNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessChargebackJob(
    ILogger<ProcessChargebackJob> logger,
    IChargeBackNotificationService chargeBackNotificationService)
    : IJob<ChargebackNotificationPayload>
{
    /// <summary>
    /// Executa o processamento da notificação de chargeback.
    /// </summary>
    /// <summary>
    /// Executa o processamento da notificação de chargeback.
    /// </summary>
    public async Task ExecuteAsync(ChargebackNotificationPayload? chargebackData)
    {
        if (chargebackData == null || string.IsNullOrEmpty(chargebackData.Id))
        {
            logger.LogError("Job de Chargeback recebido com payload inválido.");
            return; // Não relança para evitar retentativas desnecessárias
        }

        logger.LogInformation(
            "Iniciando processamento do job para o Chargeback ID: {ChargebackId}", 
            chargebackData.Id
        );

        try
        {
            // Validação de formato do ID
            if (!long.TryParse(chargebackData.Id, out _))
            {
                logger.LogError(
                    "ID do Chargeback não é um número válido: {Id}", 
                    chargebackData.Id
                );
                return; // Não relança para evitar retentativas desnecessárias
            }

            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Buscar detalhes na API do MP
            // 2. Verificar se chargeback já existe
            // 3. Atualizar Payment e Subscription via Repositories
            // 4. Criar/Atualizar Chargeback via Repository
            // 5. Commit via UnitOfWork
            // 6. Enviar email
            await chargeBackNotificationService.VerifyAndProcessChargeBackAsync(chargebackData);

            logger.LogInformation("Job Chargeback {Id} concluído com sucesso.", chargebackData.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no Job Chargeback {Id}.", chargebackData.Id);
            throw; // Relança para que o Hangfire aplique a política de retentativas
        }
    }
}