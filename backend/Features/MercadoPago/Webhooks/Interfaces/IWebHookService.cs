using MeuCrudCsharp.Models;

// Namespace do seu modelo de notificação

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces
{
    public interface IWebhookService
    {
        /// <summary>
        /// Valida a assinatura da requisição do webhook do Mercado Pago.
        /// </summary>
        /// <param name="request">O objeto HttpRequest da requisição recebida.</param>
        /// <param name="notification">O corpo (body) da notificação deserializado.</param>
        /// <returns>True se a assinatura for válida, senão False.</returns>
        bool IsSignatureValid(HttpRequest request, MercadoPagoWebhookNotification notification);

        /// <summary>
        /// Processa a notificação do webhook, identificando o tipo e enfileirando para processamento.
        /// </summary>
        /// <param name="notification">O corpo (body) da notificação deserializado.</param>
        Task ProcessWebhookNotificationAsync(MercadoPagoWebhookNotification notification);
    }
}
