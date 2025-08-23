// Em: Features/Emails/ViewModels/RefundConfirmationEmailViewModel.cs

namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    public class RefundConfirmationEmailViewModel
    {
        /// <summary>
        /// Nome do usuário para personalização. Ex: "Olá, Carlos,".
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// O ID do pagamento que foi reembolsado, para referência.
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// A data em que o reembolso foi confirmado.
        /// </summary>
        public DateTime ConfirmationDate { get; set; }

        /// <summary>
        /// URL para a página "Minha Conta" ou dashboard do usuário.
        /// </summary>
        public string AccountUrl { get; set; }
    }
}
