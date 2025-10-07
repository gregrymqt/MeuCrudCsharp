using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services
{
    /// <summary>
    /// Implementa <see cref="INotificationPayment"/> para processar notificações de pagamento.
    /// Este serviço verifica o status de um pagamento no banco de dados local e envia e-mails de confirmação ou rejeição ao usuário.
    /// </summary>
    public class NotificationPaymentService : INotificationPayment
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<NotificationPaymentService> _logger;
        private readonly IMercadoPagoPaymentService _mercadoPagoService;
        private readonly IRefundNotification _refundNotification;
        private readonly ISubscriptionService _subscriptionService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NotificationPayment"/>.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        /// <param name="emailSender">O serviço para envio de e-mails.</param>
        /// <param name="razorRenderer">O serviço para renderizar templates Razor para string.</param>
        /// <param name="logger">O serviço de logging.</param>
        /// <param name="mercadoPagoService">O serviço de logging.</param>
        /// <param name="refundNotification">O serviço de logging.</param>
        public NotificationPaymentService(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<NotificationPaymentService> logger,
            IMercadoPagoPaymentService mercadoPagoService, // <-- INJEÇÃO DO SERVIÇO DO MP
            IRefundNotification refundNotification, // <-- INJEÇÃO DO SERVIÇO SIGNALR
            ISubscriptionService subscriptionService
        )
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
            _mercadoPagoService = mercadoPagoService;
            _refundNotification = refundNotification;
            _subscriptionService = subscriptionService;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Este método foi projetado para ser executado por um job em segundo plano (ex: Hangfire).
        /// Ele relança exceções para permitir que o sistema de jobs trate falhas e execute novas tentativas.
        /// </remarks>
        // Assinatura simplificada, sem o userId redundante
        public async Task VerifyAndProcessNotificationAsync(string internalPaymentId)
        {
            _logger.LogInformation(
                "Iniciando processamento de notificação para PaymentId: {PaymentId}",
                internalPaymentId
            );

            var localPayment = await SearchForPaymentAsync(internalPaymentId);
            if (localPayment == null)
                throw new ResourceNotFoundException($"Pagamento com ID {internalPaymentId} não foi encontrado.");

            var user = localPayment.User;
            if (user == null)
                throw new ResourceNotFoundException(
                    $"Usuário associado ao pagamento {internalPaymentId} não foi encontrado.");

            // 1. Busca o status mais recente do pagamento no Mercado Pago
            var externPayment = await _mercadoPagoService.GetPaymentStatusAsync(localPayment.ExternalId);
            if (externPayment == null)
            {
                _logger.LogWarning("Não foi possível obter detalhes do pagamento externo {ExternalId}",
                    localPayment.ExternalId);
                // Você pode querer lançar uma exceção aqui para que o Hangfire tente novamente.
                throw new Exception($"Falha ao obter detalhes do pagamento {localPayment.ExternalId} do Mercado Pago.");
            }

            switch (externPayment.Status)
            {
                case "approved":
                    // 3. Verifica se a assinatura JÁ EXISTE.
                    if (localPayment.Subscription == null)
                    {
                        // 3a. SE NÃO EXISTE, é um pagamento único (PIX ou Cartão)! VAMOS CRIAR A ASSINATURA.
                        _logger.LogInformation(
                            "Pagamento {PaymentId} aprovado. Nenhuma assinatura local encontrada, criando uma nova...",
                            internalPaymentId);

                        // Extrai os metadados que você salvou na criação do pagamento
                        var metadata = JsonSerializer.Deserialize<PaymentMetadata>(externPayment.ExternalReference);
                        if (metadata == null || metadata.PlanPublicId == Guid.Empty)
                        {
                            throw new InvalidOperationException(
                                $"Metadados (ExternalReference) inválidos ou ausentes no pagamento {externPayment.Id}. Não é possível criar a assinatura.");
                        }

                        // CHAMA O SERVIÇO ESPECIALIZADO PARA CRIAR A ASSINATURA NO BANCO
                        await _subscriptionService.ActivateSubscriptionFromSinglePaymentAsync(
                            user.Id,
                            metadata.PlanPublicId,
                            externPayment.Id.ToString(),
                            externPayment.Payer.Email,
                            localPayment.LastFourDigits
                        );

                        _logger.LogInformation(
                            "Assinatura de pagamento único criada com sucesso para o usuário {UserId}.",
                            user.Id);
                    }
                    else
                    {
                        // 3b. SE JÁ EXISTE, é um pagamento de uma assinatura recorrente. Apenas atualizamos o status.
                        _logger.LogInformation(
                            "Pagamento {PaymentId} aprovado. Atualizando status da assinatura existente {SubscriptionId}.",
                            internalPaymentId, localPayment.Subscription.Id);
                        localPayment.Subscription.Status = "active";
                    }

                    // Atualiza o status do pagamento local para 'approved'
                    localPayment.Status = "approved";
                    await _context.SaveChangesAsync();
                    await SendConfirmationEmailAsync(user, internalPaymentId);
                    break;

                case "rejected":
                case "cancelled":
                    localPayment.Status = externPayment.Status;
                    if (localPayment.Subscription != null)
                    {
                        localPayment.Subscription.Status = externPayment.Status;
                    }

                    await _context.SaveChangesAsync();
                    await SendRejectionEmailAsync(user, internalPaymentId);
                    break;

                case "refunded":
                    localPayment.Status = "refunded";
                    if (localPayment.Subscription != null)
                    {
                        localPayment.Subscription.Status = "refunded";
                    }

                    await _context.SaveChangesAsync();
                    await _refundNotification.SendRefundStatusUpdate(localPayment.UserId, "completed",
                        "Seu reembolso foi processado com sucesso!");
                    await SendRefundConfirmationEmailAsync(user, internalPaymentId);
                    break;

                default:
                    _logger.LogWarning("Status de pagamento não tratado recebido do Mercado Pago: {Status}",
                        externPayment.Status);
                    break;
            }
        }


        /// <summary>
        /// Busca o status de um pagamento no banco de dados local.
        /// </summary>
        /// <param name="paymentId">O ID do pagamento a ser consultado.</param>
        /// <returns>A string representando o status do pagamento, ou nulo se não encontrado.</returns>
        /// <exception cref="ArgumentException">Lançada se o <paramref name="paymentId"/> não for um GUID válido.</exception>
        /// <exception cref="AppServiceException">Lançada se ocorrer um erro ao acessar o banco de dados.</exception>
        private async Task<Models.Payments?> SearchForPaymentAsync(string paymentId)
        {
            try
            {
                var payment = await _context
                    .Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar o status do pagamento {PaymentId} no banco de dados.",
                    paymentId
                );
                throw new AppServiceException($"Falha ao consultar o pagamento {paymentId}.", ex);
            }
        }

        private async Task SendPaymentEmailNotificationAsync(Users user, string paymentId, string subject,
            string viewPath, object viewModel, string logContext)
        {
            try
            {
                var htmlBody = await _razorRenderer.RenderViewToStringAsync(viewPath, viewModel);
                // O corpo de texto puro pode ser gerado a partir do view model se você padronizá-lo
                var plainTextBody = $"Olá, {user.Name}! Novidades sobre seu pagamento {paymentId}.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation(
                    "E-mail de {LogContext} enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                    logContext, user.Email, paymentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enviar e-mail de {LogContext} para {UserEmail} (PaymentId: {PaymentId}).",
                    logContext, user.Email, paymentId
                );
                throw new ExternalApiException($"Falha ao renderizar ou enviar o e-mail de {logContext}.", ex);
            }
        }

        private async Task SendConfirmationEmailAsync(Users user, string paymentId)
        {
            var viewModel = new ConfirmationEmailViewModel { UserName = user.Name, PaymentId = paymentId };
            await SendPaymentEmailNotificationAsync(user, paymentId, "Seu pagamento foi aprovado! 🎉",
                "~/Pages/EmailTemplates/Confirmation/Email.cshtml", viewModel, "Confirmação");
        }

        private async Task SendRejectionEmailAsync(Users user, string paymentId)
        {
            var viewModel = new RejectionEmailViewModel()
            {
                UserName = user.Name, PaymentId = paymentId
            };
            await SendPaymentEmailNotificationAsync(user, paymentId, "Atenção: Ocorreu um problema com seu pagamento",
                "~/Pages/EmailTemplates/Rejection/Email.cshtml", viewModel, "Rejeição");
        }

        private async Task SendRefundConfirmationEmailAsync(Users user, string paymentId)
        {
            var viewModel = new ConfirmationEmailViewModel { UserName = user.Name, PaymentId = paymentId };
            await SendPaymentEmailNotificationAsync(user, paymentId, "Seu Reembolso foi aprovado! 🎉",
                "~/Pages/EmailTemplates/Refund/Email.cshtml", viewModel, "Confirmação de Reembolso");
        }
    }
}