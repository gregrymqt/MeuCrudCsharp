using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Linq;

namespace MeuCrudCsharp.Features.Videos.Notification
{
    public class VideoNotificationService : IVideoNotificationService
    {
        private readonly IHubContext<VideoProcessingHub> _hubContext;
        // 1. Injetar o ConnectionMapping
        private readonly ConnectionMapping<string> _mapping;

        public VideoNotificationService(
            IHubContext<VideoProcessingHub> hubContext, 
            ConnectionMapping<string> mapping) // Adicionado aqui
        {
            _hubContext = hubContext;
            _mapping = mapping;
        }

        // O parâmetro agora é a CHAVE (storageIdentifier), não o nome do grupo
        public async Task SendProgressUpdate(
            string storageIdentifier,
            string message,
            int progress,
            bool isComplete = false,
            bool isError = false)
        {
            // 2. Obter as conexões inscritas neste vídeo específico
            var connectionIds = _mapping.GetConnections(storageIdentifier).ToList();

            if (connectionIds.Any())
            {
                // 3. Enviar a atualização para as conexões corretas
                await _hubContext
                    .Clients.Clients(connectionIds)
                    .SendAsync(
                        "ProgressUpdate",
                        new
                        {
                            Message = message,
                            Progress = progress,
                            IsComplete = isComplete,
                            IsError = isError,
                        }
                    );
            }
        }
    }
}