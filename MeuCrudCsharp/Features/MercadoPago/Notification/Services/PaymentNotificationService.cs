using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using Microsoft.AspNetCore.SignalR;
using System.Linq; // Necessário para o .Any()

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IHubContext<PaymentProcessingHub> _hubContext;
        // 1. Injetar o ConnectionMapping que usa STRING como chave (para o userId)
        private readonly ConnectionMapping<string> _mapping;
        
        public PaymentNotificationService(
            IHubContext<PaymentProcessingHub> hubContext, 
            ConnectionMapping<string> mapping) // Adicionado aqui
        {
            _hubContext = hubContext;
            _mapping = mapping;
        }

        public async Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update)
        {
            // 2. Obter a lista de todas as conexões ativas para este userId
            var connectionIds = _mapping.GetConnections(userId).ToList();

            // 3. Enviar a mensagem apenas para as conexões daquele usuário específico
            if (connectionIds.Any())
            {
                await _hubContext.Clients.Clients(connectionIds)
                    .SendAsync("UpdatePaymentStatus", update);
            }
        }
    }
}