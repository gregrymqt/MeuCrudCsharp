namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    /// <summary>
    /// ViewModel para o template de e-mail de notificação de recebimento de chargeback.
    /// </summary>
    public class ChargebackReceivedEmailViewModel
    {
        public string UserName { get; }
        public long ChargebackId { get; }
        public string SupportUrl { get; }

        public ChargebackReceivedEmailViewModel(
            string userName,
            long chargebackId,
            string supportUrl
        )
        {
            UserName = userName;
            ChargebackId = chargebackId;
            SupportUrl = supportUrl;
        }
    }
}