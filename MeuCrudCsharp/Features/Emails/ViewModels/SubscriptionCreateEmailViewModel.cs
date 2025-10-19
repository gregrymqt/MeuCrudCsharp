namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    /// <summary>
    /// ViewModel para o template de e-mail de confirmação de criação de assinatura.
    /// </summary>
    public class SubscriptionCreateEmailViewModel
    {
        public string UserName { get; }
        public string PlanName { get; }
        public string? SubscriptionId { get; }
        public DateTime CurrentPeriodEndDate { get; }
        public string AccountUrl { get; }

        public SubscriptionCreateEmailViewModel(
            string userName,
            string planName,
            string subscriptionId,
            DateTime currentPeriodEndDate,
            string accountUrl
        )
        {
            UserName = userName;
            PlanName = planName;
            SubscriptionId = subscriptionId;
            CurrentPeriodEndDate = currentPeriodEndDate;
            AccountUrl = accountUrl;
        }
    }
}