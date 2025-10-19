using System.Linq;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Notifications
{
    public class RefundNotification : IRefundNotification
    {
        private readonly IHubContext<RefundProcessingHub> _hubContext;

        // 1. Injetar o ConnectionMapping
        private readonly ConnectionMapping<string> _mapping;

        public RefundNotification(
            IHubContext<RefundProcessingHub> hubContext,
            ConnectionMapping<string> mapping
        ) // Adicionado aqui
        {
            _hubContext = hubContext;
            _mapping = mapping;
        }

        public async Task SendRefundStatusUpdate(string userId, string status, string message)
        {
            // 2. Obter as conexões do usuário
            var connectionIds = _mapping.GetConnections(userId).ToList();

            if (connectionIds.Any())
            {
                // 3. Enviar para a lista de conexões específicas
                await _hubContext
                    .Clients.Clients(connectionIds)
                    .SendAsync("ReceiveRefundStatus", new { Status = status, Message = message });
            }
        }
    }
}
