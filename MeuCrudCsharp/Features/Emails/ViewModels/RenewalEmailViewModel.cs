namespace MeuCrudCsharp.Features.Emails.ViewModels
{
    public class RenewalEmailViewModel
    {
        public RenewalEmailViewModel(string userName, string planName, DateTime newExpirationDate, decimal transactionAmount, string accountUrl, string supportUrl)
        {
            UserName = userName;
            PlanName = planName;
            NewExpirationDate = newExpirationDate;
            TransactionAmount = transactionAmount;
            AccountUrl = accountUrl;
            SupportUrl = supportUrl;
        }

        public string UserName { get; }
        public string PlanName { get; }
        public DateTime NewExpirationDate { get; }
        public decimal TransactionAmount { get; }
        public string AccountUrl { get; }
        public string SupportUrl { get; }
    }
}