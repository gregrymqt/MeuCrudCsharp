using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.Refunds.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Notification
{
    /// <summary>
    /// Implementa <see cref="INotificationPaymentService"/> para processar notificações de pagamento.
    /// Este serviço verifica o status de um pagamento no banco de dados local e envia e-mails de confirmação ou rejeição ao usuário.
    /// </summary>
    public class NotificationPayment : INotificationPayment
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<NotificationPayment> _logger;
        private readonly IMercadoPagoPaymentService _mercadoPagoService;
        private readonly IRefundNotification _refundNotification;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NotificationPayment"/>.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        /// <param name="emailSender">O serviço para envio de e-mails.</param>
        /// <param name="razorRenderer">O serviço para renderizar templates Razor para string.</param>
        /// <param name="logger">O serviço de logging.</param>
        public NotificationPayment(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<NotificationPayment> logger,
            IMercadoPagoPaymentService mercadoPagoService, // <-- INJEÇÃO DO SERVIÇO DO MP
            IRefundNotification refundNotification // <-- INJEÇÃO DO SERVIÇO SIGNALR
        )
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
            _mercadoPagoService = mercadoPagoService;
            _refundNotification = refundNotification;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Este método foi projetado para ser executado por um job em segundo plano (ex: Hangfire).
        /// Ele relança exceções para permitir que o sistema de jobs trate falhas e execute novas tentativas.
        /// </remarks>
        public async Task VerifyAndProcessNotificationAsync(string userId, string internalPaymentId)
        {
            _logger.LogInformation(
                "Iniciando processamento de notificação para UserID: {UserId}, PaymentId: {PaymentId}",
                userId,
                internalPaymentId
            );

            try
            {
                var localPayment = await SearchForPaymentAsync(internalPaymentId);
                var user = await _context.Users.FindAsync(userId.ToString());

                if (user == null)
                    throw new ResourceNotFoundException(
                        $"Usuário com ID {userId} não foi encontrado para notificação."
                    );

                if (localPayment == null)
                    throw new ResourceNotFoundException(
                        $"Pagamento com ID {internalPaymentId} não foi encontrado para notificação."
                    );

                var externPayment = await _mercadoPagoService.GetPaymentStatusAsync(
                    localPayment.ExternalId
                );

                _logger.LogInformation(
                    "Pagamento {PaymentId} encontrado com status: {Status}",
                    internalPaymentId,
                    localPayment.Status
                );

                if (externPayment.Status == "approved")
                {
                    localPayment.Status = "approved";
                    localPayment.Subscription.Status = "active";
                    await _context.SaveChangesAsync();
                    await SendConfirmationEmailAsync(user, internalPaymentId);
                }
                else if (externPayment.Status == "rejected" || externPayment.Status == "cancelled")
                {
                    localPayment.Status = externPayment.Status;
                    await _context.SaveChangesAsync();
                    await SendRejectionEmailAsync(user, internalPaymentId);
                }
                else if (externPayment.Status == "refunded") // <-- A LÓGICA PARA O REEMBOLSO!
                {
                    _logger.LogInformation(
                        "Pagamento {PaymentId} confirmado como REEMBOLSADO. Atualizando DB e notificando via SignalR.",
                        localPayment.ExternalId
                    );

                    // 1. Atualizar o status no seu banco de dados
                    localPayment.Status = "refunded";
                    localPayment.Subscription.Status = "refunded"; // ou "cancelled", como preferir
                    await _context.SaveChangesAsync();

                    // 2. Disparar a notificação via SignalR
                    await _refundNotification.SendRefundStatusUpdate(
                        userId,
                        "completed",
                        "Seu reembolso foi processado com sucesso!"
                    );

                    await SendRefundConfirmationEmailAsync(user, internalPaymentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha crítica no processamento da notificação para UserID: {UserId}, PaymentId: {PaymentId}",
                    userId,
                    internalPaymentId
                );
                throw;
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
                    .Payments.AsNoTracking()
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

        /// <summary>
        /// Renderiza e envia um e-mail de confirmação de pagamento para o usuário.
        /// </summary>
        /// <param name="user">O usuário que receberá o e-mail.</param>
        /// <param name="paymentId">O ID do pagamento confirmado.</param>
        /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
        private async Task SendConfirmationEmailAsync(Users user, string paymentId)
        {
            try
            {
                var subject = "Seu pagamento foi aprovado! 🎉";
                var viewModel = new ConfirmationEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId,
                };
                var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                    "~/Pages/EmailTemplates/Confirmation/Email.cshtml",
                    viewModel
                );
                var plainTextBody =
                    $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi aprovado com sucesso.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation(
                    "E-mail de confirmação enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                    user.Email,
                    paymentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enviar e-mail de CONFIRMAÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                    user.Email,
                    paymentId
                );
                // Lança uma exceção específica de API externa para sinalizar o tipo de erro
                throw new ExternalApiException(
                    "Falha ao renderizar ou enviar o e-mail de confirmação.",
                    ex
                );
            }
        }

        // Repetimos o mesmo padrão para o e-mail de rejeição
        /// <summary>
        /// Renderiza e envia um e-mail de rejeição de pagamento para o usuário.
        /// </summary>
        /// <param name="user">O usuário que receberá o e-mail.</param>
        /// <param name="paymentId">O ID do pagamento rejeitado.</param>
        /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
        private async Task SendRejectionEmailAsync(Users user, string paymentId)
        {
            try
            {
                var subject = "Atenção: Ocorreu um problema com seu pagamento";
                var viewModel = new RefundConfirmationEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId,
                    ConfirmationDate = DateTime.UtcNow, // Use a data atual
                    AccountUrl = "https://seusite.com/minha-conta", // Coloque a URL real aqui
                };
                var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                    "~/Pages/EmailTemplates/Rejection/Email.cshtml",
                    viewModel
                );
                var plainTextBody =
                    $"Olá, {user.Name}. Infelizmente, ocorreu um problema com o seu pagamento de ID {paymentId} e ele foi rejeitado.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation(
                    "E-mail de rejeição enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                    user.Email,
                    paymentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enviar e-mail de REJEIÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                    user.Email,
                    paymentId
                );
                throw new ExternalApiException(
                    "Falha ao renderizar ou enviar o e-mail de rejeição.",
                    ex
                );
            }
        }

        /// <summary>
        /// Renderiza e envia um e-mail de confirmação do reembolso do pagamento para o usuário.
        /// </summary>
        /// <param name="user">O usuário que receberá o e-mail.</param>
        /// <param name="paymentId">O ID do pagamento confirmado.</param>
        /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
        private async Task SendRefundConfirmationEmailAsync(Users user, string paymentId)
        {
            try
            {
                var subject = "Seu Reembolso foi aprovado! 🎉";
                var viewModel = new ConfirmationEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId,
                };
                var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                    "~/Pages/EmailTemplates/Refund/Email.cshtml",
                    viewModel
                );
                var plainTextBody =
                    $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi Reembolsado com sucesso.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation(
                    "E-mail de confirmação de reembolso enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                    user.Email,
                    paymentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enviar e-mail de CONFIRMAÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                    user.Email,
                    paymentId
                );
                // Lança uma exceção específica de API externa para sinalizar o tipo de erro
                throw new ExternalApiException(
                    "Falha ao renderizar ou enviar o e-mail de confirmação.",
                    ex
                );
            }
        }
    }
}
