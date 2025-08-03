using System;
using System.Linq; // Adicionado para usar .Any()
using System.Threading.Tasks;
using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public class ProcessPaymentNotificationJob
    {
        private readonly ILogger<ProcessPaymentNotificationJob> _logger;
        private readonly ApiDbContext _context;
        private readonly INotificationPaymentService _notificationPaymentService;

        public ProcessPaymentNotificationJob(
            ILogger<ProcessPaymentNotificationJob> logger,
            ApiDbContext context,
            INotificationPaymentService notificationPaymentService
        )
        {
            _logger = logger;
            _context = context;
            _notificationPaymentService = notificationPaymentService;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })] // Aumentando o delay para 1 minuto
        public async Task ExecuteAsync(string paymentId)
        {
            // MUDANÇA 1: Validação "Fail-Fast" antes de qualquer outra coisa.
            // Se o ID for inválido, o job falha imediatamente sem consumir recursos.
            if (string.IsNullOrEmpty(paymentId))
            {
                _logger.LogError("Job de notificação de pagamento recebido com um PaymentId nulo ou vazio. O job será descartado.");
                // Lançar exceção evita que o Hangfire tente reprocessar um job inválido.
                throw new ArgumentNullException(nameof(paymentId), "O ID do pagamento não pode ser nulo.");
            }

            _logger.LogInformation("Iniciando processamento do job para o Payment ID: {PaymentId}", paymentId);

            // A transação é a melhor forma de garantir a consistência dos dados.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // MUDANÇA 2: Corrigindo o nome da tabela na consulta SQL
                // E simplificando a busca com .FirstOrDefault() que pode ser nulo.
                var pagamentoLocal = await _context
                    .Payments
                    .FromSqlRaw(@"SELECT * FROM ""Payments"" WHERE ""ExternalId"" = {0} FOR UPDATE", paymentId)
                    .FirstOrDefaultAsync();

                if (pagamentoLocal == null)
                {
                    // MUDANÇA 3: Lançar uma exceção específica em vez de retornar vazio.
                    // Isso informa ao Hangfire que algo está errado (ex: o webhook chegou antes do pagamento ser salvo).
                    // O retry do Hangfire pode resolver isso na próxima tentativa.
                    throw new ResourceNotFoundException($"Pagamento com ID externo {paymentId} não encontrado no banco. Tentando novamente mais tarde.");
                }

                // A verificação de reprocessamento está ótima.
                var statusProcessaveis = new[] { "pendente", "iniciando" };
                if (!statusProcessaveis.Contains(pagamentoLocal.Status))
                {
                    _logger.LogInformation(
                        "Pagamento {PaymentId} já foi processado (Status: {Status}). Finalizando job com sucesso.",
                        paymentId,
                        pagamentoLocal.Status
                    );
                    await transaction.CommitAsync();
                    return; // Encerra o job com sucesso, sem erros.
                }

                await _notificationPaymentService.VerifyAndProcessNotificationAsync(
                    pagamentoLocal.UserId,
                    pagamentoLocal.Id.ToString() // Passando o ID interno
                );

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Processamento do Payment ID: {PaymentId} concluído com sucesso.",
                    paymentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar notificação para o Payment ID: {PaymentId}. A transação será revertida.",
                    paymentId
                );

                await transaction.RollbackAsync();

                // Relançar a exceção é crucial para que o Hangfire saiba que o job falhou
                // e possa aplicar a política de retentativas.
                throw;
            }
        }
    }
}