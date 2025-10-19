using System;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

public interface IClaimNotificationService
{
    Task VerifyAndProcessClaimAsync(ClaimNotificationPayload claimPayload);
}
