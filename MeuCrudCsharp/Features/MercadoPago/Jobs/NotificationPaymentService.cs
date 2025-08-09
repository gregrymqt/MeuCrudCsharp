using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    /// <summary>
    /// Implementa <see cref="INotificationPaymentService"/> para processar notificações de pagamento.
    /// Este serviço verifica o status de um pagamento no banco de dados local e envia e-mails de confirmação ou rejeição ao usuário.
    /// </summary>
    public class NotificationPaymentService : INotificationPaymentService
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<NotificationPaymentService> _logger;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NotificationPaymentService"/>.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        /// <param name="emailSender">O serviço para envio de e-mails.</param>
        /// <param name="razorRenderer">O serviço para renderizar templates Razor para string.</param>
        /// <param name="logger">O serviço de logging.</param>
        public NotificationPaymentService(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<NotificationPaymentService> logger
        )
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Este método foi projetado para ser executado por um job em segundo plano (ex: Hangfire).
        /// Ele relança exceções para permitir que o sistema de jobs trate falhas e execute novas tentativas.
        /// </remarks>
        public async Task VerifyAndProcessNotificationAsync(Guid userId, string paymentId)
        {
            _logger.LogInformation(
                "Iniciando processamento de notificação para UserID: {UserId}, PaymentId: {PaymentId}",
                userId,
                paymentId
            );

            try
            {
                var status = await SearchForStatusAsync(paymentId);
                var user = await _context.Users.FindAsync(userId.ToString());

                if (user == null)
                    throw new ResourceNotFoundException(
                        $"Usuário com ID {userId} não foi encontrado para notificação."
                    );

                if (status == null)
                    throw new ResourceNotFoundException(
                        $"Pagamento com ID {paymentId} não foi encontrado para notificação."
                    );

                _logger.LogInformation(
                    "Pagamento {PaymentId} encontrado com status: {Status}",
                    paymentId,
                    status
                );

                if (status == "approved")
                {
                    await SendConfirmationEmailAsync(user, paymentId);
                }
                else if (status == "rejected" || status == "cancelled")
                {
                    await SendRejectionEmailAsync(user, paymentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha crítica no processamento da notificação para UserID: {UserId}, PaymentId: {PaymentId}",
                    userId,
                    paymentId
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
        private async Task<string?> SearchForStatusAsync(string paymentId)
        {
            try
            {
                if (!Guid.TryParse(paymentId, out var paymentGuid))
                {
                    throw new ArgumentException($"O PaymentId '{paymentId}' não é um GUID válido.");
                }

                var payment = await _context
                    .Payments.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == paymentGuid);

                return payment?.Status;
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
                var viewModel = new RejectionEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId,
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
    }
}
