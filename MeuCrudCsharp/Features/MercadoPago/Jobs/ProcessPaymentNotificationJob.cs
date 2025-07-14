// Em Jobs/ProcessPaymentNotificationJob.cs

using System.Threading.Tasks;
using Hangfire;
using MeuCrudCsharp.Data;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// Executa o job. Este método seria chamado pelo seu sistema de filas.
        /// O atributo [AutomaticRetry] é um exemplo de como o Hangfire lida com retentativas.
        /// </summary>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10 })]
        public async Task ExecuteAsync(string paymentId)
        {
            _logger.LogInformation(
                "Iniciando processamento do job para o Payment ID: {PaymentId}",
                paymentId
            );

            // Inicia uma transação no banco de dados.
            // Se algo der errado aqui dentro, tudo é revertido (rollback).
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );

            try
            {
                // Busca o pagamento no seu banco de dados E TRAVA A LINHA.
                // Esta é a tradução de ->lockForUpdate().
                // A sintaxe exata do SQL pode variar (ex: "WITH (UPDLOCK)" para SQL Server).
                // Este exemplo usa a sintaxe do PostgreSQL.
                var pagamentoLocal = await _context
                    .Payment_User.FromSqlRaw(
                        @"SELECT * FROM ""Payment_User"" WHERE ""PaymentId"" = {0} FOR UPDATE",
                        paymentId
                    )
                    .FirstOrDefaultAsync();

                if (pagamentoLocal == null)
                {
                    _logger.LogWarning(
                        "Pagamento com ID {PaymentId} não encontrado no banco de dados. O job será descartado.",
                        paymentId
                    );
                    await transaction.CommitAsync(); // Comita a transação vazia e encerra.
                    return;
                }

                // Se o pagamento já foi processado (status não é mais 'pendente'), não faz nada.
                // Esta verificação previne o reprocessamento.
                if (pagamentoLocal.Status != "pendente" && pagamentoLocal.Status != "iniciando")
                {
                    _logger.LogInformation(
                        "Pagamento {PaymentId} já foi processado (Status: {Status}). Pulando job.",
                        paymentId,
                        pagamentoLocal.Status
                    );
                    await transaction.CommitAsync(); // Libera o "cadeado" e termina o job.
                    return;
                }

                // Se chegou até aqui, o pagamento é novo ou ainda está pendente.
                // Chama seu serviço que vai verificar na API do MP e atualizar o banco.
                await _notificationPaymentService.VerifyAndProcessNotificationAsync(paymentId);

                // A transação é concluída aqui (commit) e o "cadeado" é liberado.
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

                // Reverte todas as alterações no banco de dados.
                await transaction.RollbackAsync();

                // Lança a exceção novamente para que o sistema de filas (Hangfire, etc.)
                // saiba que o job falhou e possa tentar novamente.
                throw;
            }
        }
    }
}
