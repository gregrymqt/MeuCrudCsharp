namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IVideoProcessingService
    {
        Task ProcessVideoToHlsAsync(string inputFilePath, string outputDirectory, string storageIdentifier);
    }
}
