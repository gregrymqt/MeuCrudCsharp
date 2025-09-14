using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.MercadoPago.Services
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IHubContext<PaymentProcessingHub> _hubContext;

        public PaymentNotificationService(IHubContext<PaymentProcessingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update)
        {
            var groupName = $"payment-status-user-{userId}";

            // O nome do evento que o JavaScript vai ouvir será "PaymentStatusUpdate"
            return _hubContext.Clients.Group(groupName)
                .SendAsync("PaymentStatusUpdate", update);
        }
    }
}
