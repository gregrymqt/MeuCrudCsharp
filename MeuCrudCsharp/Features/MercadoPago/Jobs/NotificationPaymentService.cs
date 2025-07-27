// Services/NotificationPaymentService.cs
using System;
using System.Threading.Tasks;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public class NotificationPaymentService : INotificationPaymentService
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender; // O "Carteiro"
        private readonly IRazorViewToStringRenderer _razorRenderer;

        public NotificationPaymentService(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer
        )
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
        }

        /// <summary>
        /// Método principal que orquestra o processo.
        /// </summary>
        public async Task VerifyAndProcessNotificationAsync(Guid userId, string paymentId)
        {
            // 1. Busca o status do pagamento no nosso banco de dados
            var status = await SearchForStatusAsync(paymentId);
            var user = await _context.Users.FindAsync(userId);

            if (user == null || status == null)
            {
                // Logar um erro aqui: usuário ou pagamento não encontrado
                return;
            }

            // 2. Decide qual email enviar com base no status
            if (status == "approved")
            {
                await SendConfirmationEmailAsync(user, paymentId);
            }
            else if (status == "rejected" || status == "cancelled")
            {
                await SendRejectionEmailAsync(user, paymentId);
            }
        }

        /// <summary>
        /// Busca o status de um pagamento no banco de dados.
        /// </summary>
        private async Task<string?> SearchForStatusAsync(string paymentId)
        {
            var payment = await _context
                .Payment_User.AsNoTracking() // Usa AsNoTracking para uma consulta mais rápida de apenas leitura
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            return payment?.Status;
        }

        /// <summary>
        /// Prepara e solicita o envio do email de confirmação de pagamento.
        /// </summary>
        private async Task SendConfirmationEmailAsync(Users user, string paymentId)
        {
            var subject = "Seu pagamento foi aprovado! 🎉";

            // Cria o modelo com os dados para a view
            var viewModel = new ConfirmationEmailViewModel
            {
                UserName = user.Name,
                PaymentId = paymentId,
            };

            // Renderiza a view .cshtml para uma string de HTML
            var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                "~/Pages/EmailTemplates/Confirmation/Email.cshtml",
                viewModel
            );

            // Cria a versão em texto puro como fallback
            var plainTextBody =
                $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi aprovado com sucesso.";

            await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
        }

        /// <summary>
        /// Prepara e solicita o envio do email de rejeição de pagamento.
        /// </summary>
        private async Task SendRejectionEmailAsync(Users user, string paymentId)
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
        }
    }
}
