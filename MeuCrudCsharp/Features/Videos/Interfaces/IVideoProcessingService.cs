using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that processes video files.
    /// </summary>
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Asynchronously processes a source video file, converting it into the HLS (HTTP Live Streaming) format.
        /// </summary>
        /// <param name="inputFilePath">The full path to the source video file to be processed.</param>
        /// <param name="outputDirectory">The directory where the resulting HLS manifest (.m3u8) and segment (.ts) files will be saved.</param>
        /// <param name="storageIdentifier">The unique identifier for the video, used to update its status in the database upon completion or failure.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous processing operation.</returns>
        Task ProcessVideoToHlsAsync(
            string inputFilePath,
            string outputDirectory,
            string storageIdentifier
        );
    }
}
