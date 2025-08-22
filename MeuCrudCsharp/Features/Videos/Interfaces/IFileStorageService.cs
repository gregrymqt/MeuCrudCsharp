namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IFileStorageService
    {
        Task<string?> SaveThumbnailAsync(IFormFile? file);
        Task DeleteThumbnailAsync(string? thumbnailUrl);
        Task DeleteVideoAssetsAsync(string storageIdentifier);
    }
}
