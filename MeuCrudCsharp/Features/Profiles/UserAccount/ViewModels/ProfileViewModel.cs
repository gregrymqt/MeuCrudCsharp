using System.Collections.Generic;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Models; // Necessário para o Payment

namespace MeuCrudCsharp.Features.Profiles.UserAccount.ViewModels
{
    public class ProfileViewModel
    {
        public UserProfileDto? UserProfile { get; set; }
        public SubscriptionDetailsDto? Subscription { get; set; }
        public IEnumerable<Models.Payments?> PaymentHistory { get; set; } // NOVO
    }
}
