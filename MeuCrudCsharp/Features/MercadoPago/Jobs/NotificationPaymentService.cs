using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções customizadas
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // MUDANÇA 1: Adicionando o Logger

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public class NotificationPaymentService : INotificationPaymentService
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<NotificationPaymentService> _logger; // MUDANÇA 1

        public NotificationPaymentService(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<NotificationPaymentService> logger) // MUDANÇA 1
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
        }

        public async Task VerifyAndProcessNotificationAsync(Guid userId, string paymentId)
        {
            _logger.LogInformation("Iniciando processamento de notificação para UserID: {UserId}, PaymentId: {PaymentId}", userId, paymentId);

            // MUDANÇA 2: Envolvendo o método principal em um try-catch
            try
            {
                var status = await SearchForStatusAsync(paymentId);
                var user = await _context.Users.FindAsync(userId.ToString());

                // MUDANÇA 3: Substituindo falha silenciosa por uma exceção clara
                if (user == null)
                    throw new ResourceNotFoundException($"Usuário com ID {userId} não foi encontrado para notificação.");

                if (status == null)
                    throw new ResourceNotFoundException($"Pagamento com ID {paymentId} não foi encontrado para notificação.");

                _logger.LogInformation("Pagamento {PaymentId} encontrado com status: {Status}", paymentId, status);

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
                _logger.LogError(ex, "Falha crítica no processamento da notificação para UserID: {UserId}, PaymentId: {PaymentId}", userId, paymentId);
                // Relança a exceção para que o Hangfire saiba que o job falhou e deve tentar novamente.
                throw;
            }
        }

        private async Task<string?> SearchForStatusAsync(string paymentId)
        {
            try
            {
                // A conversão de string para Guid pode falhar
                if (!Guid.TryParse(paymentId, out var paymentGuid))
                {
                    throw new ArgumentException($"O PaymentId '{paymentId}' não é um GUID válido.");
                }

                var payment = await _context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == paymentGuid);

                return payment?.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o status do pagamento {PaymentId} no banco de dados.", paymentId);
                throw new AppServiceException($"Falha ao consultar o pagamento {paymentId}.", ex);
            }
        }

        private async Task SendConfirmationEmailAsync(Users user, string paymentId)
        {
            // MUDANÇA 4: Adicionando try-catch específico para o envio de e-mail
            try
            {
                var subject = "Seu pagamento foi aprovado! 🎉";
                var viewModel = new ConfirmationEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId,
                };
                var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                    "~/Pages/EmailTemplates/Confirmation/Email.cshtml", viewModel);
                var plainTextBody = $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi aprovado com sucesso.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation("E-mail de confirmação enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.", user.Email, paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de CONFIRMAÇÃO para {UserEmail} (PaymentId: {PaymentId}).", user.Email, paymentId);
                // Lança uma exceção específica de API externa para sinalizar o tipo de erro
                throw new ExternalApiException("Falha ao renderizar ou enviar o e-mail de confirmação.", ex);
            }
        }

        // Repetimos o mesmo padrão para o e-mail de rejeição
        private async Task SendRejectionEmailAsync(Users user, string paymentId)
        {
            try
            {
                var subject = "Atenção: Ocorreu um problema com seu pagamento";
                var viewModel = new RejectionEmailViewModel
                {
                    UserName = user.Name,
                    PaymentId = paymentId
                };
                var htmlBody = await _razorRenderer.RenderViewToStringAsync("~/Pages/EmailTemplates/Rejection/Email.cshtml", viewModel);
                var plainTextBody = $"Olá, {user.Name}. Infelizmente, ocorreu um problema com o seu pagamento de ID {paymentId} e ele foi rejeitado.";

                await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                _logger.LogInformation("E-mail de rejeição enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.", user.Email, paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de REJEIÇÃO para {UserEmail} (PaymentId: {PaymentId}).", user.Email, paymentId);
                throw new ExternalApiException("Falha ao renderizar ou enviar o e-mail de rejeição.", ex);
            }
        }
    }
}