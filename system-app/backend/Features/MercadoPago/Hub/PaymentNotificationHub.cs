using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.MercadoPago.Hub
{
    public class PaymentNotificationHub(
        IHubContext<PaymentProcessingHub> hubContext,
        ConnectionMapping<string> mapping)
        : IPaymentNotificationHub
    {
        // 1. Injetar o ConnectionMapping que usa STRING como chave (para o userId)

        // Adicionado aqui

        public async Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update)
        {
            // 2. Obter a lista de todas as conexões ativas para este userId
            var connectionIds = mapping.GetConnections(userId).ToList();

            // 3. Enviar a mensagem apenas para as conexões daquele usuário específico
            if (connectionIds.Count != 0)
            {
                await hubContext
                    .Clients.Clients(connectionIds)
                    .SendAsync("UpdatePaymentStatus", update);
            }
        }
    }
}
