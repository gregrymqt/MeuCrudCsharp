using System.Linq;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Notifications
{
    public class RefundNotification(
        IHubContext<RefundProcessingHub> hubContext,
        ConnectionMapping<string> mapping)
        : IRefundNotification
    {
        // 1. Injetar o ConnectionMapping

        // Adicionado aqui

        public async Task SendRefundStatusUpdate(string userId, string status, string message)
        {
            // 2. Obter as conexões do usuário
            var connectionIds = mapping.GetConnections(userId).ToList();

            if (connectionIds.Count != 0)
            {
                // 3. Enviar para a lista de conexões específicas
                await hubContext
                    .Clients.Clients(connectionIds)
                    .SendAsync("ReceiveRefundStatus", new { Status = status, Message = message });
            }
        }
    }
}
