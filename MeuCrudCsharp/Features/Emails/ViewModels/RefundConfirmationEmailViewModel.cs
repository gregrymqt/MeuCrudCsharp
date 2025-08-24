// Em: Features/Emails/ViewModels/RefundConfirmationEmailViewModel.cs

namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    public class RefundConfirmationEmailViewModel
    {
        public string UserName { get; set; }
        public string PaymentId { get; set; }
        public DateTime ConfirmationDate { get; set; }
        public string AccountUrl { get; set; }

        /// <summary>
        /// URL principal do seu site (para o logo e rodapé).
        /// </summary>
        public string SiteUrl { get; set; }
    }
}
