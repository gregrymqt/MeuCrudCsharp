using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    public class VideoProcessingHub : Hub
    {
        // Cliente chama este método para entrar em um grupo e receber atualizações
        public async Task SubscribeToJobProgress(string storageIdentifier)
        {
            // O nome do grupo é único para cada trabalho de processamento
            var groupName = $"processing-{storageIdentifier}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
