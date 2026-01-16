using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Models;

// Namespace do seu modelo de notificação

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces
{
    public interface IWebhookService
    {
        bool IsSignatureValid(
            HttpRequest request,
            MercadoPagoWebhookNotification notification
        );

        Task ProcessWebhookNotificationAsync(
            MercadoPagoWebhookNotification notification
        );
    }
}
