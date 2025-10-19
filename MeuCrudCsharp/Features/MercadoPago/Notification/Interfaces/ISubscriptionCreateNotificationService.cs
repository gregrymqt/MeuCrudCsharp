using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

public interface ISubscriptionCreateNotificationService
{
    Task VerifyAndProcessSubscriptionAsync(Subscription subscription);
}
