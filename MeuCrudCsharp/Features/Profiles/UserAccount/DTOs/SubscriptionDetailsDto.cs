namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs
{
    public class SubscriptionDetailsDto
    {
        public string SubscriptionId { get; set; } // ID do MP
        public string PlanName { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string LastFourCardDigits { get; set; }
        public DateTime? NextBillingDate { get; set; }
    }
}
