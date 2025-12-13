using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

public class ProcessPlanSubscriptionJob : IJob<PaymentNotificationData>
{
    private readonly ApiDbContext _context;
    private readonly IPlanUpdateNotificationService _planUpdateNotificationService;
    private readonly ILogger<ProcessPlanSubscriptionJob> _logger;

    public ProcessPlanSubscriptionJob(
        ApiDbContext context,
        IPlanUpdateNotificationService planUpdateNotificationService,
        ILogger<ProcessPlanSubscriptionJob> logger
    )
    {
        _context = context;
        _planUpdateNotificationService = planUpdateNotificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(PaymentNotificationData resource)
    {
        if (resource?.Id is null)
        {
            _logger.LogWarning(
                "O ID do recurso de notificação de pagamento está nulo. A execução do job foi cancelada."
            );
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );

        try
        {
            // Trava a linha para evitar que outros jobs processem o mesmo plano simultaneamente.
            // A sintaxe SQL pode variar dependendo do seu banco de dados (ex: "FOR UPDATE" para PostgreSQL/MySQL).
            var plan = await _context
                .Plans.FromSqlRaw(
                    "SELECT * FROM Plans WITH (UPDLOCK, ROWLOCK) WHERE ExternalId = {0}",
                    resource.Id
                )
                .FirstOrDefaultAsync();

            if (plan is null)
            {
                _logger.LogInformation(
                    "Plano com ExternalId {ExternalId} não encontrado. O job será encerrado.",
                    resource.Id
                );
                await transaction.RollbackAsync(); // Libera a transação, embora nada tenha sido feito.
                return;
            }

            await _planUpdateNotificationService.VerifyAndProcessPlanUpdate(resource.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ocorreu um erro ao processar a assinatura do plano com ExternalId {ExternalId}.",
                resource.Id
            );
            await transaction.RollbackAsync();
            throw; // Re-lança a exceção para que o Hangfire possa tentar novamente de acordo com sua política.
        }
    }
}
