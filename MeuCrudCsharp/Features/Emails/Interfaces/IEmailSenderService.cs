using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Emails.Interfaces
{
    /// <summary>
    /// Define o contrato para um serviço responsável pelo envio de e-mails.
    /// </summary>
    public interface IEmailSenderService
    {
        /// <summary>
        /// Envia um e-mail de forma assíncrona para um destinatário.
        /// </summary>
        /// <param name="to">O endereço de e-mail do destinatário.</param>
        /// <param name="subject">O assunto do e-mail.</param>
        /// <param name="htmlBody">O corpo do e-mail em formato HTML.</param>
        /// <param name="plainTextBody">O corpo do e-mail em formato de texto simples, como alternativa ao HTML.</param>
        /// <returns>Uma <see cref="Task"/> que representa a operação de envio assíncrona.</returns>
        Task SendEmailAsync(string to, string subject, string htmlBody, string plainTextBody);
    }
}
