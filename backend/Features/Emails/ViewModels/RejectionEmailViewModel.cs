namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    public class RejectionEmailViewModel
    {
        public string UserName { get; set; }
        public string PaymentId { get; set; }

        /// <summary>
        /// URL para a página de pagamento para o usuário tentar novamente.
        /// </summary>
        public string PaymentPageUrl { get; set; }

        /// <summary>
        /// URL principal do seu site (para o logo e rodapé).
        /// </summary>
        public string SiteUrl { get; set; }
    }
}
