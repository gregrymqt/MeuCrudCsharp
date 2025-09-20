using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Notifications
{
    public class RefundNotification : IRefundNotification
    {
        private readonly IHubContext<RefundProcessingHub> _hubContext;

        public RefundNotification(IHubContext<RefundProcessingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendRefundStatusUpdate(string userId, string status, string message)
        {
            var groupName = $"user-{userId}";

            // Envia uma mensagem para um método no cliente chamado "ReceiveRefundStatus"
            // para todos os membros do grupo (ou seja, todas as abas abertas daquele usuário).
            await _hubContext
                .Clients.Group(groupName)
                .SendAsync(
                    "ReceiveRefundStatus",
                    new
                    {
                        Status = status, // ex: "completed", "failed"
                        Message = message,
                    }
                );
        }
    }
}
