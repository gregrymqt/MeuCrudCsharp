// Features/Hubs/RefundProcessingHub.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    [Authorize]
    public class RefundProcessingHub : Hub
    {
        private readonly ConnectionMapping<string> _mapping;

        // 1. Injetamos nosso serviço de mapeamento genérico
        public RefundProcessingHub(ConnectionMapping<string> mapping)
        {
            _mapping = mapping;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // 2. Usamos o mapper para registrar a conexão do usuário
                _mapping.Add(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // 3. Na desconexão, apenas pedimos ao mapper para remover a conexão.
            // A lógica de encontrar a qual usuário ela pertencia está centralizada no mapper.
            _mapping.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
