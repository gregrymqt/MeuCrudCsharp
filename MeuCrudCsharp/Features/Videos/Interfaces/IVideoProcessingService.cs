using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that processes video files.
    /// </summary>
    public interface IVideoProcessingService
    {
        Task ProcessVideoToHlsAsync(string storageIdentifier, string originalFileName);
    }
}
