using Hangfire;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de atualização de cartão de crédito.
/// Delega a lógica de negócio para o CardUpdateNotificationService.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60])]
public class ProcessCardUpdateJob(
    ILogger<ProcessCardUpdateJob> logger,
    ICardUpdateNotificationService cardUpdateNotificationService)
    : IJob<CardUpdateNotificationPayload>
{
    /// <summary>
    /// Executa o processamento da notificação de atualização de cartão.
    /// </summary>
    /// <param name="cardUpdateData">O payload da notificação recebida do webhook.</param>
    public async Task ExecuteAsync(CardUpdateNotificationPayload? cardUpdateData)
    {
        if (cardUpdateData == null || string.IsNullOrEmpty(cardUpdateData.CustomerId))
        {
            logger.LogError(
                "Job de atualização de cartão recebido com payload nulo ou CustomerId inválido. O job será descartado."
            );
            // Não relança a exceção para evitar retentativas desnecessárias.
            return;
        }

        logger.LogInformation(
            "Iniciando processamento do job para atualização de cartão do cliente: {CustomerId}",
            cardUpdateData.CustomerId
        );

        try
        {
            // Delega TODA a lógica para o serviço especializado
            // O service é responsável por:
            // 1. Validar se existe assinatura ativa
            // 2. Buscar dados do cartão na API do MP
            // 3. Atualizar assinatura via Repository
            // 4. Commit via UnitOfWork
            // 5. Enviar email
            await cardUpdateNotificationService.VerifyAndProcessCardUpdate(cardUpdateData);

            logger.LogInformation(
                "Processamento de atualização de cartão para o cliente {CustomerId} concluído com sucesso.",
                cardUpdateData.CustomerId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar a atualização de cartão para o cliente {CustomerId}.",
                cardUpdateData.CustomerId
            );
            throw; // Relança para que o Hangfire aplique a política de retentativas.
        }
    }
}
