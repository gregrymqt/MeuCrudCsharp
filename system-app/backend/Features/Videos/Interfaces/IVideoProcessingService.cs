namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    /// <summary>
    /// Define o contrato para um serviço que processa vídeos em formato HLS.
    /// </summary>
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Processa um vídeo para formato HLS (HTTP Live Streaming) usando FFmpeg.
        /// </summary>
        /// <param name="videoId">ID interno do vídeo no banco de dados.</param>
        /// <param name="fileId">ID do arquivo original (MP4) no banco de dados.</param>
        Task ProcessVideoToHlsAsync(int videoId, int fileId);
    }
}
