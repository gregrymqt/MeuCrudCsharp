using System;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;

public interface IPlanUpdateNotificationService
{
    Task VerifyAndProcessPlanUpdate(string externalId);
}
