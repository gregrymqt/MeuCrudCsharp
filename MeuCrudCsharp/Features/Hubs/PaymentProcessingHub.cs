// Features/Hubs/PaymentProcessingHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Hubs
{
    public class PaymentProcessingHub : Hub
    {
        /// <summary>
        /// Cliente chama este método para se inscrever e receber atualizações de status
        /// de pagamento para um usuário específico.
        /// </summary>
        /// <param name="userId">O ID do usuário que está fazendo o pagamento.</param>
        public async Task SubscribeToPaymentStatus(string userId)
        {
            // O nome do grupo será algo como "payment-status-user-123"
            var groupName = $"payment-status-user-{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}