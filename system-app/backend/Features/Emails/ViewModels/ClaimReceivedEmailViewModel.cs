namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    /// <summary>
    /// ViewModel para o template de e-mail de notificação de recebimento de claim.
    /// </summary>
    public class ClaimReceivedEmailViewModel
    {
        public string UserName { get; }
        public long ClaimId { get; }
        public string AccountUrl { get; }
        public string SupportUrl { get; }

        public ClaimReceivedEmailViewModel(
            string userName,
            long claimId,
            string accountUrl,
            string supportUrl
        )
        {
            UserName = userName;
            ClaimId = claimId;
            AccountUrl = accountUrl;
            SupportUrl = supportUrl;
        }
    }
}
