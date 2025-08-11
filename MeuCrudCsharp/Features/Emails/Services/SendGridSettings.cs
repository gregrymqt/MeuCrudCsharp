namespace MeuCrudCsharp.Features.Emails.Services
{
    /// <summary>
    /// Representa as configurações para o serviço SendGrid.
    /// </summary>
    public class SendGridSettings
    {
        /// <summary>
        /// O nome da seção de configuração no appsettings.json ou user secrets.
        /// </summary>
        public const string SectionName = "SendGrid";

        /// <summary>
        /// Obtém ou define a chave da API do SendGrid.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define o endereço de e-mail para enviar e-mails.
        /// </summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define o nome a ser exibido para o endereço de e-mail 'de'.
        /// </summary>
        public string FromName { get; set; } = string.Empty;
    }
}
