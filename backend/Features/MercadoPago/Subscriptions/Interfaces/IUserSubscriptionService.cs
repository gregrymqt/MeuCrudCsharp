using System;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

public interface IUserSubscriptionService
{
    Task<SubscriptionDetailsDto?> GetMySubscriptionDetailsAsync();
    Task ChangeSubscriptionStatusAsync(string newStatus);
}
