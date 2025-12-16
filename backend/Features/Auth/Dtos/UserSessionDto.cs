using System;

namespace MeuCrudCsharp.Features.Auth.Dtos
{
    public class UserSessionDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public SubscriptionDto? Subscription { get; set; }
        public List<PaymentHistoryDto> LastPayments { get; set; } = new();
    }

    public class SubscriptionDto
    {
        public string Status { get; set; } // Active, Canceled, etc.
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class PaymentHistoryDto
    {
        public decimal Amount { get; set; }
        public DateTime? DateApproved { get; set; }
        public string Status { get; set; }
        public string Method { get; set; } // CreditCard, Pix, etc.
        public string LastFourDigits { get; set; }
    }
}
