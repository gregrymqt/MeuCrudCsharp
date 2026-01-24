// Features/Hubs/PaymentProcessingHub.cs
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    [Authorize]
    public class PaymentProcessingHub(ConnectionMapping<string> mapping) : Hub
    {
        // 1. Injetamos o mesmo serviço de mapeamento

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // 2. Registramos a conexão do usuário no mapper
                mapping.Add(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // 3. Removemos a conexão usando o mapper
            mapping.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
