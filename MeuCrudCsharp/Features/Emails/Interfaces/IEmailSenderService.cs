namespace MeuCrudCsharp.Features.Emails.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string plainTextBody);
    }
}
