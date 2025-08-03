using System.Threading.Tasks;
using MeuCrudCsharp.Features.Emails.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MeuCrudCsharp.Features.Emails.Services
{
    public class SendGridEmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _configuration;

        public SendGridEmailSenderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string plainTextBody
        )
        {
            // Pega a chave da API do appsettings.json ou de variáveis de ambiente
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress("seu-email@seudominio.com", "Nome da Sua Empresa");
            var toAddress = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextBody, htmlBody);

            var response = await client.SendEmailAsync(msg);

            // Aqui você pode logar se o email foi enviado com sucesso ou não
        }
    }
}