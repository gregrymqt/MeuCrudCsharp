using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

/// <summary>
/// Job do Hangfire para processar notificações de atualização de cartão de crédito.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
public class ProcessCardUpdateJob : IJob<CardUpdateNotificationPayload>
{
    private readonly ILogger<ProcessCardUpdateJob> _logger;
    private readonly ApiDbContext _context;
    private readonly ICardUpdateNotificationService _cardUpdateNotificationService;

    public ProcessCardUpdateJob(
        ILogger<ProcessCardUpdateJob> logger,
        ApiDbContext context,
        ICardUpdateNotificationService cardUpdateNotificationService
    )
    {
        _logger = logger;
        _context = context;
        _cardUpdateNotificationService = cardUpdateNotificationService;
    }

    /// <summary>
    /// Executa o processamento da notificação de atualização de cartão.
    /// </summary>
    /// <param name="cardUpdateData">O payload da notificação recebida do webhook.</param>
    public async Task ExecuteAsync(CardUpdateNotificationPayload cardUpdateData)
    {
        if (cardUpdateData == null || string.IsNullOrEmpty(cardUpdateData.CustomerId))
        {
            _logger.LogError(
                "Job de atualização de cartão recebido com payload nulo ou CustomerId inválido. O job será descartado."
            );
            // Não relança a exceção para evitar retentativas desnecessárias.
            return;
        }

        _logger.LogInformation(
            "Iniciando processamento do job para atualização de cartão do cliente: {CustomerId}",
            cardUpdateData.CustomerId
        );

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Verifica se existe um usuário com o CustomerId fornecido
            var userExists = await _context
                .Users.AsNoTracking()
                .AnyAsync(u => u.CustomerId == cardUpdateData.CustomerId);

            if (!userExists)
            {
                _logger.LogWarning(
                    "Nenhum usuário encontrado com o CustomerId: {CustomerId}. A notificação será ignorada.",
                    cardUpdateData.CustomerId
                );
                await transaction.CommitAsync(); // Comita a transação vazia para finalizar o job
                return;
            }

            // Delega para o serviço especializado realizar a lógica de negócio.
            await _cardUpdateNotificationService.VerifyAndProcessCardUpdate(cardUpdateData);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation(
                "Processamento de atualização de cartão para o cliente {CustomerId} concluído com sucesso.",
                cardUpdateData.CustomerId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar a atualização de cartão para o cliente {CustomerId}. A transação será revertida.",
                cardUpdateData.CustomerId
            );
            await transaction.RollbackAsync();
            throw; // Relança a exceção para que o Hangfire aplique a política de retentativas.
        }
    }
}
