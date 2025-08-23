using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    [Authorize]
    public class RefundProcessingHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Pega o ID do usuário logado a partir do token (claim)
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // Adiciona a conexão atual a um grupo específico para este usuário.
                // Ex: "user-abc-123"
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnConnectedAsync();
        }

        // Opcional: Limpar quando o usuário desconectar
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
