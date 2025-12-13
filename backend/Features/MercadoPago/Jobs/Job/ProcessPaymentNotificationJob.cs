﻿using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Utils;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.EntityFrameworkCore;

// Nossas exceções

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job
{
    /// <summary>
    /// Representa um job do Hangfire responsável por processar uma notificação de pagamento recebida.
    /// Este job garante a consistência transacional, a idempotência e a lógica de retentativas para o processamento de pagamentos.
    /// </summary>
    public class ProcessPaymentNotificationJob : IJob<PaymentNotificationData>
    {
        private readonly ILogger<ProcessPaymentNotificationJob> _logger;
        private readonly ApiDbContext _context;
        private readonly INotificationPayment _notificationPayment;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ProcessPaymentNotificationJob"/>.
        /// </summary>
        /// <param name="logger">O serviço de logging.</param>
        /// <param name="context">O contexto do banco de dados para operações transacionais.</param>
        /// <param name="notificationPayment">O serviço que contém a lógica de negócio para processar a notificação.</param>
        /// <param name="cacheService"></param>
        public ProcessPaymentNotificationJob(
            ILogger<ProcessPaymentNotificationJob> logger,
            ApiDbContext context,
            INotificationPayment notificationPayment,
            ICacheService cacheService
        )
        {
            _logger = logger;
            _context = context;
            _notificationPayment = notificationPayment;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Executa a lógica de processamento da notificação de pagamento.
        /// </summary>
        /// <remarks>
        /// Este método é invocado pelo Hangfire. O atributo <see cref="AutomaticRetryAttribute"/>
        /// garante que o job será reexecutado em caso de falha, com um atraso de 60 segundos entre as 3 tentativas.
        /// A lógica utiliza uma transação de banco de dados e um bloqueio de linha (FOR UPDATE) para garantir
        /// a consistência e evitar condições de corrida.
        /// </remarks>
        /// <param name="paymentId">O ID externo do pagamento a ser processado.</param>
        /// <exception cref="ArgumentNullException">Lançada se o <paramref name="paymentId"/> for nulo ou vazio.</exception>
        /// <exception cref="ResourceNotFoundException">Lançada se o pagamento não for encontrado no banco de dados, acionando uma nova tentativa do Hangfire.</exception>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })] // Aumentando o delay para 1 minuto
        public async Task ExecuteAsync(PaymentNotificationData resource)
        {
            if (string.IsNullOrEmpty(resource?.Id))
            {
                _logger.LogError(
                    "Job de notificação de pagamento recebido com um PaymentId nulo ou vazio. O job será descartado."
                );
                throw new ArgumentNullException(
                    nameof(resource.Id),
                    "O ID do pagamento não pode ser nulo."
                );
            }

            _logger.LogInformation(
                "Iniciando processamento do job para o Payment ID: {PaymentId}",
                resource.Id
            );

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pagamentoLocal = await _context
                    .Payments.FromSqlRaw(
                        "SELECT * FROM Payments WITH (UPDLOCK, ROWLOCK) WHERE ExternalId = {0}",
                        resource.Id
                    )
                    .FirstOrDefaultAsync();

                if (pagamentoLocal == null)
                {
                    throw new ResourceNotFoundException(
                        $"Pagamento com ID externo {resource.Id} não encontrado no banco. Tentando novamente mais tarde."
                    );
                }

                var statusProcessaveis = new[]
                {
                    InternalPaymentStatus.Pendente,
                    InternalPaymentStatus.Iniciando,
                };
                if (!statusProcessaveis.Contains(pagamentoLocal.Status))
                {
                    _logger.LogInformation(
                        "Pagamento {PaymentId} já foi processado (Status: {Status}). Finalizando job com sucesso.",
                        resource.Id,
                        pagamentoLocal.Status
                    );
                    await transaction.CommitAsync();
                    return;
                }

                await _notificationPayment.VerifyAndProcessNotificationAsync(
                    pagamentoLocal.Id // Passando o ID interno
                );

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Processamento do Payment ID: {PaymentId} concluído com sucesso.",
                    resource.Id
                );

                var cacheKey = $"payment:db:{pagamentoLocal.Id}";
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogInformation("Cache invalidado para a chave: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar notificação para o Payment ID: {PaymentId}. A transação será revertida.",
                    resource.Id
                );

                await transaction.RollbackAsync();

                // Relança a exceção para que o Hangfire saiba que o job falhou e aplique a política de retentativas.
                throw;
            }
        }
    }
}
