using Microsoft.AspNetCore.SignalR;

namespace MeuCrudCsharp.Features.Hubs
{
    public class VideoProcessingHub : Hub
    {
        private readonly ConnectionMapping<string> _mapping;

        public VideoProcessingHub(ConnectionMapping<string> mapping)
        {
            _mapping = mapping;
        }

        public async Task SubscribeToJobProgress(string storageIdentifier)
        {
            var groupName = $"processing-{storageIdentifier}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _mapping.Add(storageIdentifier, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Agora funciona! Pegamos a chave (storageIdentifier) a partir da conexão.
            var key = _mapping.GetKey(Context.ConnectionId);

            if (key != null)
            {
                var groupName = $"processing-{key}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                _mapping.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
