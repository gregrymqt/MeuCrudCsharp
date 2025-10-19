namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    public class ConfirmationEmailViewModel
    {
        public string? UserName { get; set; }
        public string? PaymentId { get; set; }

        /// <summary>
        /// URL para o conteúdo que o usuário acabou de comprar.
        /// </summary>
        public string? AccessContentUrl { get; set; }

        /// <summary>
        /// URL principal do seu site (para o logo e rodapé).
        /// </summary>
        public string? SiteUrl { get; set; }
    }
}
