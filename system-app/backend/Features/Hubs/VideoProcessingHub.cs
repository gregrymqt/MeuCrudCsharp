using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    public class VideoProcessingHub(ConnectionMapping<string> mapping) : Hub
    {
        public async Task SubscribeToJobProgress(string storageIdentifier)
        {
            var groupName = $"processing-{storageIdentifier}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            mapping.Add(storageIdentifier, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Agora funciona! Pegamos a chave (storageIdentifier) a partir da conexão.
            var key = mapping.GetKey(Context.ConnectionId);

            if (key != null)
            {
                var groupName = $"processing-{key}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                mapping.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
