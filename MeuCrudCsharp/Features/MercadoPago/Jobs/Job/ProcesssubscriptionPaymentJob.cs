using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
    public class ProcessSubscriptionPaymentJob : IJob<PaymentNotificationData>
    {
        private readonly ILogger<ProcessSubscriptionPaymentJob> _logger;
        private readonly ApiDbContext _context;
        private readonly ISubscriptionNotificationService _subscriptionNotificationService;

        public ProcessSubscriptionPaymentJob(
            ILogger<ProcessSubscriptionPaymentJob> logger,
            ApiDbContext context,
            ISubscriptionNotificationService subscriptionNotificationService
        )
        {
            _logger = logger;
            _context = context;
            _subscriptionNotificationService = subscriptionNotificationService;
        }

        public async Task ExecuteAsync(PaymentNotificationData resource)
        {
            var resourceId = resource?.Id;

            _logger.LogInformation(
                "Iniciando o processamento do pagamento da assinatura com ResourceId: {ResourceId}",
                resourceId
            );

            // Validação básica do recurso
            if (string.IsNullOrEmpty(resourceId))
            {
                _logger.LogError(
                    "ResourceId (ID do Pagamento) está nulo ou vazio. O job não será reprocessado."
                );
                return;
            }

            // Inicia a transação no banco de dados
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Busca e TRAVA a assinatura no banco para evitar condições de corrida.
                //    O 'resourceId' é o ID do pagamento, então buscamos a assinatura por esse ID.
                var subscription = await _context
                    .Subscriptions.FromSqlRaw(
                        "SELECT * FROM \"Subscriptions\" WHERE \"PaymentId\" = {0} FOR UPDATE",
                        resourceId
                    )
                    .Include(s => s.Plan) // Inclui o Plano para pegar os detalhes de frequência
                    .FirstOrDefaultAsync();

                if (subscription == null)
                {
                    // Se a assinatura não for encontrada, não há o que fazer. O job não deve falhar para não ser reprocessado.
                    _logger.LogWarning(
                        "Nenhuma assinatura encontrada para o pagamento com ID: {ResourceId}. Finalizando job.",
                        resourceId
                    );
                    await transaction.CommitAsync(); // Comita a transação vazia
                    return;
                }

                // 2. Garante a idempotência: se a assinatura já foi renovada (data futura), encerra.
                //    Esta verificação evita o reprocessamento do mesmo pagamento.
                if (subscription.CurrentPeriodEndDate > DateTime.UtcNow)
                {
                    _logger.LogInformation(
                        "Assinatura {SubscriptionId} já parece ter sido renovada (Data de Expiração: {ExpirationDate}). Finalizando job.",
                        subscription.Id,
                        subscription.CurrentPeriodEndDate
                    );
                    await transaction.CommitAsync();
                    return;
                }

                // 3. CHAMA O SERVIÇO para executar a regra de negócio
                await _subscriptionNotificationService.ProcessRenewalAsync(subscription);

                // 4. Salva as alterações e comita a transação
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Job para pagamento {ResourceId} da assinatura {SubscriptionId} concluído com sucesso.",
                    resourceId,
                    subscription.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar o pagamento {ResourceId}. A transação será revertida.",
                    resourceId
                );
                await transaction.RollbackAsync();
                throw; // Relança a exceção para o Hangfire saber que o job falhou e deve tentar novamente.
            }
        }
    }
}
